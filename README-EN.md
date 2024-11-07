# Introduction to CN-GreenLumaGUI :

![](./Pictures/icon.png)

Developed with WPF, this application manages the application list folder for the Steam tool [“GreenLuma”](https://cs.rin.ru/forum/viewtopic.php?f=10&t=103709)

A standalone, installation-free file allows complete operations with just mouse clicks.

The primary purpose of its creation is to enable friends who are completely unfamiliar with computers to operate GreenLuma to unlock games.

As it uses WPF, it requires the .NET runtime and runs exclusively on Windows systems.

## Interface Display：

![GUI-GameList](./imgs/gui-0-en.png)

![GUI-Search](./imgs/gui-1-en.png)

![GUI-AppendGame](./imgs/gui-2-en.png)

![GUI-Options](./imgs/gui-3-en.png)

## FAQ :

#### Q: How do I use it? Which button do I press?

A: First, add some games in the software, then select the games you want to unlock in the "Game List," and click "Start Steam." Just wait for Steam to launch.

Can't find the "Start Steam" button? It's probably now called "Close Steam" because Steam is already running.

* Steam must be launched via the button; manually launching Steam doesn't count. You can close this application after launching Steam. *

* Starting Steam first and then selecting games won't count. If you change your game selections, you need to close Steam and then relaunch it through the software for the changes to take effect. *


#### Q: Can this game be unlocked?

A: I don't know.

With an immense variety of games available, it's impossible for me to have played and tested every game you might play. For the question of whether "a certain game can be played," the only criterion is practice.

Though I can't tell you which games are playable, I can tell you which definitely aren't—

Games that need to redirect to a third-party platform definitely won't work, such as Ubisoft's (e.g., Assassin's Creed) or EA's games (e.g., Battlefield).

Online games that require constant internet connection probably won't work (e.g., Rainbow Six Siege, also a Ubisoft game).

Denuvo-encrypted games definitely won't work.

The indicators of incompatibility are: 

(1) even when up-to-date, the game continuously prompts for updates, indicating it won't work, and 

(2) if the game displays an "application error" message upon launching, it means it won't work.

However, under special circumstances, some online games might work — if you've shared a game from someone else, and it happens to lack verification, you could potentially join online games through Steam. Interestingly, most single-player games with multiplayer features work this way.

If I already have shared games, why go through this process?

Because sometimes, it's not just you who wants to play. Steam's "Family Sharing" function doesn't actually share games; it's more like lending out a game. Meaning, if you've shared your library with five people, only one person can play at a time. The rest have no games to borrow.

In other words, Steam's game sharing allows only one person among you and five friends to play at a time. Six people playing together online is impossible. However, if you unlock a game using this software, even if someone else is already playing it, as long as you still have sharing privileges, you can play normally.

Of course, games that the developer has set as unsharable on Steam, like GTA5, won't work.


#### Q: I found that in the new Family Sharing, we have only one copy of a game, and it prompts that it can't be launched when multiple people try to play at the same time.

There are four possibilities.

1: In the new Family Sharing version, the order of play is important. Have all family members who were playing this game quit first. The person who owns the game should not unlock it. Instead, have other family members who don't own but want to play use a tool to unlock and open Steam. Then everyone can enter the game together.

2: It's possible that an update in Steam has reduced the actual limit of how many games can be unlocked. Try removing the unlocks for other games and DLCs to free up space, then only unlock the game you want to play. Wait for a software update to fix the issue.

3: There might be a genuine issue with certain games, which would require an update from the original developer for the DLL file. Generally, these issues are resolved in the next version of the developer's update. I'll have to wait for that too.

4: If you're trying to play a testing branch of the game, this problem might occur as the software may not support test versions.


#### Q: I've opened the software, but I can't see any games in my library, nor can I download games.

A: This is the normal state of operation for this software. The reality is that the download function doesn't work.

If you thought this software could let you download games you don't own, you might have misunderstood its functionality.

Close the software and open Steam normally. Use cloud storage, someone else's account, share from someone else, or other means to download the games you want to play.


#### Q: The library shows I have DLC, but why can't I unlock it in-game?

A: If the game checks online whether you've purchased the DLC, there's nothing you can do, regardless of what Steam shows.

For single-player games, more often than not, it's because there's no "download" function.

Some DLCs can be unlocked because, for the game, it's just a marker. All game content is already downloaded to your computer; you just need to trick the game into thinking you have the DLC, and you can play.

But for many games, if you haven't purchased the DLC, it won't download the game files. Since you can't download, lacking game files means even if the game lets you play extra content, you can't play without those files.

In theory, if you could download the DLC by borrowing someone else's Steam account that has purchased the DLC, then you could play it.


#### Q: Some of the DLC is unlocked, as if it's cut off in the middle of a pack, with successful unlocks before a certain point and failed ones after.

A: This may be due to a Steam update reducing the limit of unlocks. You can temporarily fix this by un-checking other unlocked items to make space for the DLC.

Ultimately, waiting for a software update should resolve the issue.


#### Q: After clicking to start the game, even waiting a million years, the game doesn't launch, staying stuck on starting.

A: Compatibility mode has a small bug but allows more computers to start Steam normally. So, I've left it on by default.

In the settings panel at the very bottom, turn off compatibility mode and administrator mode. If you can still open Steam normally after that, then you can open the game normally.


#### Q: What should I do if I encounter a launch exception stating "The system cannot execute the specified program"?

Note: In rare cases, this message may appear in your language (based on system language).

A: This issue is likely related to Windows Defender. Checking [this issue](https://github.com/clinlx/CN_GreenLumaGUI/issues/12) might help clarify:

It mentions that adding the directory "C:\tmp\exewim2oav.addy.vlz" to the whitelist in Windows Security Center resolved the problem.

However, you should first ensure that the prerequisite VC++ runtime library is correctly installed.


#### Q: What should I do if I get an "Access is denied" launch exception?

Note: In rare cases, this message may appear in your language (based on system language).

A: This problem seems to occur quite frequently. First, make sure to update to the latest version (to confirm this issue still exists in the current version).

There have been resolved cases before, but I only know some of the reasons, which may not apply to all situations:

1. (Speculation) The direct cause of this error might be "insufficient permissions." There could indeed be a permissions issue, so try adjusting the Steam launch permissions to "Administrator" at the bottom of the settings page.
2. Ensure that the prerequisite VC++ runtime library is correctly installed.
3. This issue might be caused by third-party antivirus software blocking the operation. Try temporarily disabling it?
4. If your system version is too old (e.g., Windows 7) and compatibility mode isn't enabled, this might cause the problem.
5. If you've successfully launched it before but can't now, it's possible that a previous process didn't close properly and is still using file resources. Try restarting your computer.
6. If you've checked everything above and still can't find the cause, the last resort is to try launching in compatibility mode, then exit the software, and finally manually run C:\tmp\exewim2oav.addy.vlz\DLLInjector_bak.exe to see what kind of prompt the system gives.


#### Q: Why is the limit 129 games?

A: Since Greenluma isn't open source, the maximum of 129 unlocks is a hardcoded limit by the author, for unknown reasons. It used to be unlimited, but the author likely encountered a bug and thus imposed this limit. I can't resolve this.


#### Q: I've been able to use this software, but suddenly today, Steam won't pop up when I open it.

A: I've encountered this situation too but don't know why. It might be that Steam is stuck. Some people I asked said closing the software and logging into Steam normally, changing to a different account, or restarting the computer a few times might solve the issue. You can try these methods. Since the cause is unidentified and the impact is minor, it's not pursued further. (If a problem arises, first check if other games also can't be unlocked, to rule out game-specific issues.)


#### Q: Can it be used on non-Windows platforms?

A: No. It's impossible. From the original GreenLuma software to the DLL injector, to WPF, none can be used outside the Windows platform.


#### Q: I've encountered various odd issues and just can't open Steam.

A: I've tried my best to ensure the software installs and runs without issues on all the machines I could test. However, strange bugs are still numerous. In the logs, I've even seen errors due to "the absence of cmd.exe in the Windows system," a problem for which I can find no cause unless you allow me to remote into your computer to compile the code and try.

Switching compatibility mode in settings can solve some problems.

To ensure continuity, this software directly hides or discards many of GreenLuma's logs and prompts, which may affect pinpointing the issue's cause. Thus, if all else fails, you might consider abandoning my software shell and directly using the original GreenLuma to launch Steam, as it might more clearly indicate where the problem lies.

For any other questions or unresolved issues, feel free to submit an issue on GitHub to inform me.


## Built-in Files :

For immediate use, the following files are included (in base64 encoded form):

DLLInjector.exe

GreenLuma_2024_x86.dll


## Libraries Used :

[AngleSharp](https://github.com/AngleSharp/AngleSharp)

[MaterialDesignThemes](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit)

CommunityToolkit.Mvvm

Newtonsoft.Json