# MultiprocessLauncher
Launch multiple processes and move them to the selected screen. App is designed to launch multiple browser windows (args must contain `--new-window` tag).

## Options

```
-f <path> load options from a file (each line consists a process to run)

-p <proc>   process to run
-a <args>   arguments to pass to the process
-s <screen> move process window to the screen
```

#### Example:
`-p "C:\Program Files (x86)\Google\Chrome\Application\chrome.exe" -a "--new-window www.google.com" -screen 1`

## Launch on startup
To launch on startup copy a shortcut to Windows startup folder. Run (Win+R) command `shell:startup` to open startup folder location.
In shortcut properties specify process to run or path to a config file.
