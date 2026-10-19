# Putting Math Runner on an Android phone

Already set up in the project (2026-10-19):

| Setting | Value |
|---|---|
| App name | Math Runner |
| Package id | `com.marvxs.mathrunner` (it can't change after the first Play Store upload, so change it now if you want a different one) |
| Version | 1.0.0 (version code 1) |
| Screen | Landscape only, turns with the phone; nothing is drawn under a notch or camera hole |
| Icon | `Assets/_Project/Art/UI/app_icon.png` (chalkboard + runner) |
| Scenes in the build | MainMenu, LevelSelect, Game (the sandboxes stay out) |
| Engine | Unity 6000.3.11f1, IL2CPP, ARM64, minimum Android 7.1 (API 25) |

The Android module is already installed in your Unity.

## 1. Try it on your own phone (APK)

1. **On the phone**, turn on developer mode:
   - Go to *Settings > About phone* and tap **Build number** 7 times.
   - Then go to *Settings > Developer options* and turn on **USB debugging**.
2. **Make the build:** in Unity, choose the menu **Math Runner > Build Android APK (test on a phone)**.
   - The first build takes a while, roughly 5–15 minutes, because Unity converts every asset for Android.
   - When it's done, Explorer opens `Builds/Android/MathRunner-1.0.0.apk`.
3. **Install it.** Either:
   - **USB:** plug the phone in and accept the "Allow USB debugging?" prompt. In *File > Build Profiles > Android*, pick your phone under **Run Device** and click **Build And Run**.
   - **No cable:** copy the `.apk` to the phone (Google Drive, USB, chat app), tap it, and allow "Install unknown apps" for that app when asked.
4. **Check by hand on the phone.** Unity couldn't test these from the editor:
   - tapping the answer circles;
   - tapping anywhere else to jump;
   - the pause button;
   - music volume;
   - that the board never covers the runner.

## 2. Publish on Google Play (App Bundle)

1. **Make an upload key (once).** Open *Edit > Project Settings > Player > Android > Publishing Settings*, then *Keystore Manager > Create new*.
   - Save the `.keystore` **outside the project folder** and back it up.
   - Keep the passwords somewhere safe. If you lose the key or its passwords, you can't publish updates.
   - Never commit the keystore to git.
2. **Build the bundle:** choose **Math Runner > Build Android App Bundle (Google Play)**. This gives you `Builds/Android/MathRunner-1.0.0.aab`.
3. **Google Play Console** (<https://play.google.com/console>; a one-time USD 25 developer registration):
   - Create the app and choose **Free**.
   - Upload the `.aab` to **Testing > Internal testing** first, add your own email as a tester, and install it from the Play link.
   - Fill in the store listing:
     - a short and a full description;
     - the 512×512 icon (`app_icon.png`);
     - a 1024×500 feature graphic;
     - at least 2 phone screenshots (landscape is fine).
   - Fill in the **App content** forms: privacy policy, ads (none), content rating questionnaire, target audience and data safety.
   - **Kids:** the game is for children, so Google's *Families* policy applies:
     - Pick the real age groups in *Target audience*.
     - You need a privacy policy URL even though the game collects nothing. Progress is saved only on the phone, with no ads, accounts or analytics. A simple page saying that is enough; a free Google Sites page works.
   - When internal testing looks good, promote the release to **Production** and send it for review. The first review can take several days.
4. **Every later update:** raise the version in *Project Settings > Player*:
   - **Version**, e.g. 1.0.1;
   - **Bundle Version Code**, e.g. 2. It must go up by at least 1 each upload.

   Then build the bundle again with the same keystore.

## 3. Before a real release (optional polish)

- **Volume and mute switch.** There's no settings screen yet: music is fixed at about 45 % and quieter while paused.
- **Older phones.** If the game feels slow on an older phone, lower *Project Settings > Quality* for Android and turn off the Focus post-processing there.
- **App size.** The two music tracks are streamed and Vorbis-compressed, so they add only a few MB.
- **iOS.** It's not set up. It needs a Mac with Xcode and a paid Apple developer account (USD 99 a year).
