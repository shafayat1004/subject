#!/usr/bin/perl
# Apply EggShell iOS signing hygiene to a hand-added RN ios/ project so a personal
# DEVELOPMENT_TEAM never lands in git and the deployment target is uniform.
#
# The scaffold (`eggshell create-app`) does NOT generate an ios/ tree; iOS projects are
# added by hand. Run this once after adding ios/ (and any time you want to re-normalize):
#
#     Meta/LibScaffolding/scripts/wire-ios-signing.pl <app>/ios/<Proj>.xcodeproj/project.pbxproj [deploymentTarget]
#
# What it does (idempotent -- re-running is a no-op):
#   1. Removes every hardcoded DEVELOPMENT_TEAM (any value, any nesting).
#   2. Normalizes every IPHONEOS_DEPLOYMENT_TARGET to <deploymentTarget> (default 18.0).
#   3. Drops ios/Config.xcconfig (from Config.xcconfig.template beside this script) if
#      missing, and attaches it as the PROJECT-level baseConfigurationReference on the
#      project's Debug + Release configs. Target-level base configs (CocoaPods) are left
#      untouched, so `pod install` never warns and pods keep their own xcconfig.
#
# Config.xcconfig `#include?`s ios/Local.xcconfig (git-ignored) where each developer puts
# `DEVELOPMENT_TEAM = <team>`. Committed files stay identity-free. Also ensure the app
# .gitignore ignores `ios/**/Local.xcconfig` and `*.mobileprovision` (the scaffold
# .gitignore.template already does).
use strict; use warnings;
use File::Basename qw(dirname);
use File::Spec;

my ($path, $target) = @ARGV;
die "usage: wire-ios-signing.pl <project.pbxproj> [deploymentTarget]\n" unless $path && -f $path;
$target //= '18.0';

# ios/ dir is two levels up from project.pbxproj (ios/<Proj>.xcodeproj/project.pbxproj)
my $iosDir  = dirname(dirname($path));
my $cfgPath = File::Spec->catfile($iosDir, 'Config.xcconfig');
my $tmpl    = File::Spec->catfile(dirname(File::Spec->rel2abs($0)), 'Config.xcconfig.template');

# Ensure ios/Config.xcconfig exists (copy from template).
unless (-f $cfgPath) {
  die "template missing: $tmpl\n" unless -f $tmpl;
  local $/; open my $t, '<', $tmpl or die "open $tmpl: $!"; my $c = <$t>; close $t;
  open my $o, '>', $cfgPath or die "write $cfgPath: $!"; print $o $c; close $o;
  print "CREATED $cfgPath\n";
}

# Mint a fresh 24-char uppercase-hex pbxproj id.
sub newid {
  open my $r, '<', '/dev/urandom' or die "urandom: $!";
  binmode $r; read $r, my $b, 12; close $r;
  return uc unpack 'H*', $b;
}

local $/; open my $fh, '<', $path or die "open $path: $!"; my $s = <$fh>; close $fh;
my $orig = $s;

# 1. strip all hardcoded DEVELOPMENT_TEAM lines
$s =~ s/^\t+DEVELOPMENT_TEAM = [^;]+;\n//mg;

# 2. normalize deployment target everywhere
$s =~ s/IPHONEOS_DEPLOYMENT_TARGET = [^;]+;/IPHONEOS_DEPLOYMENT_TARGET = $target;/g;

# 3. attach project-level base config (skip if a Config.xcconfig ref already exists)
unless ($s =~ /\/\* Config\.xcconfig \*\//) {
  my $cfg = newid();
  my $ref = "\t\t$cfg /* Config.xcconfig */ = {isa = PBXFileReference; lastKnownFileType = text.xcconfig; path = Config.xcconfig; sourceTree = \"<group>\"; };\n";
  $s =~ s/(\/\* Begin PBXFileReference section \*\/\n)/$1$ref/
    or die "could not find PBXFileReference section\n";

  my ($mg) = $s =~ /mainGroup = ([0-9A-Fa-f]+);/ or die "no mainGroup\n";
  $s =~ s/(\n\t\t\Q$mg\E = \{\n\t\t\tisa = PBXGroup;\n\t\t\tchildren = \(\n)/$1\t\t\t\t$cfg \/* Config.xcconfig *\/,\n/
    or die "could not find mainGroup $mg children\n";

  my ($listUuid) = $s =~ /buildConfigurationList = ([0-9A-Fa-f]+) \/\* Build configuration list for PBXProject/
    or die "no PBXProject buildConfigurationList\n";
  my ($listBlock) = $s =~ /\n\t\t\Q$listUuid\E [^\n]*= \{(.+?)\n\t\t\};/s
    or die "no config list block $listUuid\n";
  my @cfgs = $listBlock =~ /([0-9A-Fa-f]{24}) \/\* (?:Debug|Release) \*\//g;
  die "expected 2 project configs, got @{[scalar @cfgs]}\n" unless @cfgs == 2;

  for my $u (@cfgs) {
    $s =~ s/(\n\t\t\Q$u\E \/\* (?:Debug|Release) \*\/ = \{\n\t\t\tisa = XCBuildConfiguration;\n)/$1\t\t\tbaseConfigurationReference = $cfg \/* Config.xcconfig *\/;\n/
      or die "could not patch project config $u\n";
  }
}

if ($s eq $orig) { print "NOCHANGE $path\n"; exit 0; }
open my $out, '>', $path or die "write $path: $!"; print $out $s; close $out;
print "WROTE $path\n";
