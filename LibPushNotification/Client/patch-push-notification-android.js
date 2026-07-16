const fs = require("fs");
const path = require("path");

const buildGradlePath = path.join(
    __dirname,
    "node_modules/react-native-push-notification/android/build.gradle"
);

if (!fs.existsSync(buildGradlePath)) {
    process.exit(0);
}

let contents = fs.readFileSync(buildGradlePath, "utf8");

contents = contents.replace(
    /implementation "\$appCompatLibName:\$supportLibVersion"/,
    'implementation "androidx.appcompat:appcompat:1.6.1"'
);

contents = contents.replace(
    /implementation "com.google.firebase:firebase-messaging:\$\{safeExtGet\('firebaseMessagingVersion', '21.1.0'\)\}"/,
    'implementation "com.google.firebase:firebase-messaging:${safeExtGet(\'firebaseMessagingVersion\', \'23.4.0\')}"'
);

fs.writeFileSync(buildGradlePath, contents);
