// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
const vscode = require('vscode');
const child_process = require('child_process');

// This method is called when your extension is activated
// Your extension is activated the very first time the command is executed

/**
 * @param {vscode.ExtensionContext} context
 */
function activate(context) {
	// Use the console to output diagnostic information (console.log) and errors (console.error)
	// This line of code will only be executed once when your extension is activated
	console.log('Congratulations, your extension "msIdentity" is now active!');

	// The command has been defined in the package.json file
	// Now provide the implementation of the command with  registerCommand
	// The commandId parameter must match the command field in package.json
	let disposable = vscode.commands.registerCommand('msIdentity.AppSync', runProcess);

	context.subscriptions.push(disposable);
}

async function runProcess() {
    console.log('running process...');

    let outputChannel = vscode.window.createOutputChannel("Example Channel");

    var input;

    const param1 = await vscode.window.showInputBox({
  prompt: 'Enter the tenant ID for the app registration',
  value: '',
    });

    if (param1 != '') {
        input = [`--tenant-id`, `${param1}`]
    }
    else {
        input = []
    }

    console.log(input);

	let rootPath = vscode.workspace.workspaceFolders[0].uri.fsPath;
    console.log(`root path: ${rootPath}`);

    const process = child_process.spawn('msidentity-app-sync', input, { cwd: rootPath });
	
    process.stdout.on('data', data => {
        console.log(`stdout: ${data}`);
        outputChannel.appendLine(data);
    });

    process.stderr.on('data', data => {
        console.error(`stderr: ${data}`);
    });

    process.on('close', code => {
        console.log(`child process exited with code ${code}`);
        outputChannel.show();
    });
}

// This method is called when your extension is deactivated
function deactivate() {}

module.exports = {
	activate,
	deactivate
}