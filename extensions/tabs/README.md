### Instructions

1. In a console, run the command: `tsc -w`

    This will run the TypeScript compiler and have it constantly monitor for any changes to the *.tsc* files.

    Any changes made to those will automatically be propogated to the */dist* folder where the `.tsx` files are converted to `.js` and referenced by the `NLUResults.html`.

2. In a console, run the command: `live-server .`

    This will run a dev server from the current location, open a new tab in your browser, and allow you to browse the pages

3. Navigate to `nluresults.html` to view the charts


### How to debug in Chrome, real-time

1. Follow the instructions above

2. Create a `launch.json` file, per the instructions in the documentation.

Mine looks like this:

```json 
{
    // Use IntelliSense to learn about possible attributes.
    // Hover to view descriptions of existing attributes.
    // For more information, visit: https://go.microsoft.com/fwlink/?linkid=830387
    "version": "0.2.0",
    "configurations": [
        {
            "type": "chrome",
            "request": "launch",
            "name": "Launch Chrome against localhost",
            "url": "http://localhost:8080/",
            "webRoot": "${workspaceFolder}"
        }
    ]
}

```

3. Read the **Attach** section of the [Code Debugger for Chrome docs](https://marketplace.visualstudio.com/items?itemName=msjsdiag.debugger-for-chrome) and attach the debugger to Chrome.

3. Press *Debug* or **F5** to begin debugging. A new Chrome window will open. 

4. Set a breakpoint on any of the `.ts` files you are working on
----------------

**Written by:** Dave Voyles, Sept-12-2019
