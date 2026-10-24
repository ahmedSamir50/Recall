Status: open

A comparable local file search keeps a tray icon on Windows and Linux and a menu-bar icon on Mac, opens its search panel with a global shortcut, keeps the process alive when the window closes, and puts Quit in that menu. Recall does not copy that design's source, and the tray itself is not in the tree.

Recall v1 shells are `android-arm64`, `ios-arm64`, `win-x64`, and `maccatalyst-arm64`. linux-x64 is a v1 RID. The Linux shell is not in the tree. A Linux tray, sign-in start, and search shortcut are open and are not in the tree. On Android and iOS a tray, sign-in start, and a global shortcut are decided-absent. On Windows and Mac Catalyst those three are not in the tree. No preferences key stores them. [App.xaml.cs](../../src/Recall.Maui/App.xaml.cs) calls `ScopeWatchCoordinator.Stop` from `OnWindowDestroying`.

A later plan may add, for Windows, Linux, and Mac Catalyst, and only in this process: a notification-area icon on Windows, a menu-bar icon on Mac, hide on window close, Quit as the menu item that stops the watcher, sign-in start defaulting off, and a shortcut defaulting on that shows the window and navigates to `/`. Whether hide keeps the watcher running is not picked. A helper process stays forbidden. A daily update check stays out.

The public remote is https://github.com/ahmedSamir50/Recall. apple-compile.yml compiles ios-arm64 and maccatalyst-arm64. A physical Apple run is not a done-when. The Apple platform files stay in the tree.

Copy, not in either resx file:

Desktop
سطح المكتب

Recall stays in the notification area on Windows and Linux and in the menu bar on Mac. Closing the window leaves it there. Quit is in that menu.
يبقى ريكول في منطقة الإشعارات على ويندوز ولينكس وفي شريط القائمة على ماك. إغلاق النافذة يبقيه هناك. الإنهاء من تلك القائمة.

Open when I sign in
افتح عند تسجيل الدخول

Search shortcut
اختصار البحث

Recall could not register that shortcut.
تعذر على ريكول تسجيل هذا الاختصار.

Recall could not add itself to sign-in items.
تعذر على ريكول إضافة نفسه إلى عناصر تسجيل الدخول.

Allow Recall in System Settings to open at sign-in.
اسمح لريكول في إعدادات النظام بالفتح عند تسجيل الدخول.

This phone has no menu bar, sign-in start, or global shortcut.
هذا الهاتف بلا شريط قوائم، ولا بدء عند الدخول، ولا اختصار عام.

The search shortcut starts as Ctrl+Shift+Space on Windows and Linux, and Shift+Command+Space on Mac. You can replace Space with a letter.
يبدأ اختصار البحث بـ Ctrl+Shift+Space على ويندوز ولينكس، وShift+Command+Space على ماك. يمكنك استبدال المسافة بحرف.

Default is Ctrl+Shift+Space on Windows and Linux, and Shift+Command+Space on Mac. Replacing Space with R gives Ctrl+Shift+R and Shift+Command+R.
