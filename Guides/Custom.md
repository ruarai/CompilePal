# Custom Compile Steps

Custom compile steps allow you to run your own programs as part of the compile process.

![Custom](https://i.imgur.com/nZ3LPua.png)

When the Wait For Exit checkbox is unchecked, it will run the program asynchronously in the background. 
The next compile step will start immedietly after the program is started. 
If Read Output is enabled, it will log it to the Output window whenever it recieves output from the program.
This can cause the logs from different programs to become mixed up. If Compile Pal does not wait for the program to exit, be aware that the compile will show as finished, even if the custom program is still running.

![Order](https://i.imgur.com/QyYpBDx.png)

Custom processes can be run at any point in the compile. To reorder the compile steps, click the Order tab to switch to the Order view. 
Click and drag on any custom process to move it up or down in the compile process.
Processes at the top get run first.
