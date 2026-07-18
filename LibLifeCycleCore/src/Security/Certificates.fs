module LibLifeCycleCore.Certificates

open System
open System.IO
open System.Reflection
open System.Security.Cryptography
open System.Security.Cryptography.X509Certificates

// Dev-only fallback: the embedded STAR_dev_subject_app.pfx secret is not committed to this
// checkout (the EmbeddedResource is commented out in LibLifeCycleCore.fsproj). When it is absent
// we synthesize an equivalent self-signed cert (CN=*.subject.app) at runtime so the Orleans
// silo/gateway TLS handshake can complete for a local DevelopmentHost. This is used only on the
// useDevelopmentCertificate = true path; production still loads real certs from the machine store.
let private generateSelfSignedDevCertificate () =
    use rsa = RSA.Create 2048
    let request =
        CertificateRequest(
            X500DistinguishedName "CN=*.subject.app",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1)

    request.CertificateExtensions.Add(X509BasicConstraintsExtension(false, false, 0, false))
    request.CertificateExtensions.Add(
        X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature ||| X509KeyUsageFlags.KeyEncipherment,
            (* critical *) false))

    let ekus = OidCollection()
    ekus.Add(Oid "1.3.6.1.5.5.7.3.1") |> ignore // server auth
    ekus.Add(Oid "1.3.6.1.5.5.7.3.2") |> ignore // client auth
    request.CertificateExtensions.Add(X509EnhancedKeyUsageExtension(ekus, (* critical *) false))

    let san = SubjectAlternativeNameBuilder()
    san.AddDnsName "*.subject.app"
    san.AddDnsName "*.dev.subject.app"
    san.AddDnsName "localhost"
    request.CertificateExtensions.Add(san.Build())

    let now = DateTimeOffset.UtcNow
    use generated = request.CreateSelfSigned(now.AddDays -1.0, now.AddYears 10)
    // Round-trip through a PFX export so the private key is usable by the TLS stack.
    let exported = generated.Export(X509ContentType.Pfx, "efefefefef")
    new X509Certificate2(exported, "efefefefef", X509KeyStorageFlags.Exportable)

let starDotDevDotSubjectDotAppTlsCertificate =
    match Assembly.GetExecutingAssembly().GetManifestResourceStream("LibLifeCycleCore.Security.STAR_dev_subject_app.pfx") with
    | null ->
        generateSelfSignedDevCertificate ()
    | stream ->
        use stream = stream
        use ms = new MemoryStream()
        stream.CopyTo(ms)
        new X509Certificate2(ms.ToArray(), "efefefefef")

let filterAndSortCertificates (now: DateTimeOffset) (certificates: seq<X509Certificate2>) : list<X509Certificate2> =
    let nowLocalDateTime = now.ToLocalTime().DateTime
    certificates
    |> Seq.where (fun x509 ->
        x509.Subject = "CN=*.subject.app"
    )
    |> Seq.where (fun x509 ->
        try
            // Attempt to access the Private Key; this might throw if we don't have access
            x509.PrivateKey |> ignore
            true
        with
        | _ -> false
    )
    |> Seq.where (fun x509 -> x509.NotAfter >= nowLocalDateTime)
    |> Seq.where (fun x509 -> x509.NotBefore <= nowLocalDateTime)
    |> Seq.sortByDescending (fun x509 ->
        x509.NotAfter
    )
    |> Seq.toList

let allSubjectDotAppTlsCertificates =
    new Lazy<list<X509Certificate2>>((fun () ->
        // lazy so it's not evaluated when useDevelopmentCertificate = true
        PlatformCertificateStore.fromCollection
            (fun certificates ->
                // FIXME - DateTime here is the time the process starts up. Ideally we should hook on to certificate expiry
                // and gracefully restart server processes as certificates expire so there is no unexpected downtime
                let now = DateTimeOffset.Now

                certificates
                |> Seq.cast<X509Certificate2>
                |> filterAndSortCertificates now)
    ), (* isThreadSafe *) true)

let getOrleansTlsCertificateAndHostName (ecosystemName: string) (useDevelopmentCertificate: bool) : X509Certificate2 * string =
    if useDevelopmentCertificate then
        (starDotDevDotSubjectDotAppTlsCertificate,
            (sprintf "%s.dev.subject.app" (ecosystemName.ToLowerInvariant())))
    else
        (List.head allSubjectDotAppTlsCertificates.Value,
            (sprintf "%s.subject.app" (ecosystemName.ToLowerInvariant())))
