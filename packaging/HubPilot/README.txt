Kandoo Store — Windows pilot
=============================

This folder installs the Kandoo Store app (MAUI, Windows). No .NET runtime
is required on this machine.

First run
---------
1. Run Kandoo Store from the Start Menu (or the desktop shortcut).
2. The first-run setup wizard asks for the store name, the owner name and
   mobile, and a manager password. No JSON editing or PowerShell is needed.
3. The store database (SQLite) lives in the app data folder, never in this
   program folder.

Hub (not in this package yet)
------------------------------
The local Store Hub ({app}\hub\Kandoo.Hub.exe) is a separate component and
is not included in this installer yet. When it ships, it runs on the local
machine and the app talks to it over loopback. The app works local-first
with its own SQLite database until then.

Data
----
Replacing this entire program folder does not remove or change the store
database in the app data folder.
