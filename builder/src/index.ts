import { execSync } from "child_process";
import fs from "fs";
import path from "path";

// Will be appended to the base directory, meaning ../.. from this script.
const csprojPaths: string[] = [`/IMLLoader`, `/ModLoaderPolyfill`, `/SMLInstaller`];
const outputPath = `/SimpleModLoaderInstaller`;

function buildCsProjects(baseDirectory: string) {
	for (const csprojPath of csprojPaths) {
		const fullCsprojPath = baseDirectory + csprojPath;

		console.log(`Building ${fullCsprojPath}`);
		try {
			execSync("dotnet build", { cwd: path.resolve(fullCsprojPath) });
		} catch (e) {
			const stdout: Uint8Array = e.stdout;
			console.error(stdout.toString());
			throw e;
		}
	}
}

function rcopy(src: string, dest: string) {
	if (!fs.existsSync(dest)) fs.mkdirSync(dest);
	fs.readdirSync(src).forEach(file => {
		const srcPath = path.join(src, file);
		const destPath = path.join(dest, file);

		if (fs.statSync(srcPath).isDirectory()) {
			rcopy(srcPath, destPath);
		} else {
			fs.copyFileSync(srcPath, destPath);
		}
	});
}

function checkWorkingDirectory() {
	const dirname = path.dirname(process.cwd());
	return dirname == "dist";
}

function build() {
	let base = "../..";
	let finalOutputPath = path.join(base, outputPath);
	// Check if we are running the script in the correct directory. If not, correct it.
	// This can only account for running the process running in the root v. dist
	if (!checkWorkingDirectory()) {
		base = "..";
		finalOutputPath = path.join(base, outputPath);
	}

	if (fs.existsSync(finalOutputPath)) {
		fs.rmSync(finalOutputPath, { recursive: true });
	}
	fs.mkdirSync(finalOutputPath);
	fs.mkdirSync(path.join(finalOutputPath, "/SML"));

	buildCsProjects(base);

	// Copy asset files
	rcopy(path.join(base, "/SMLInstaller/assets"), path.join(finalOutputPath, "/SML/assets"));
	rcopy(path.join(base, "/SMLInstaller/root_assets"), path.join(finalOutputPath, "/SML/root_assets"));

	// Copy in subproject DLLs
	fs.copyFileSync(path.join(base, "/IMLLoader/bin/Debug/netstandard2.0/IMLLoader.dll"), path.join(finalOutputPath, "/SML/IMLLoader.dll"));
	fs.copyFileSync(path.join(base, "/ModLoaderPolyfill/bin/Debug/netstandard2.0/ModLoader.dll"), path.join(finalOutputPath, "/SML/ModLoader.dll"));

	// Copy installer files
	const files = fs.readdirSync(path.join(base, "/SMLInstaller/bin/Debug/net5.0"));
	for (const file of files) {
		if (file.endsWith(".dev.json") || file.endsWith(".pdb")) continue;
		const outFileName = file; // == "SMLInstaller.exe" ? "SimpleModLoaderInstaller.exe" : file;
		fs.copyFileSync(path.join(base, "/SMLInstaller/bin/Debug/net5.0", file), path.join(finalOutputPath, "/SML", outFileName));
	}

	// Copy installer batch file
	fs.copyFileSync(path.join(base, "/builder", "/resources/install.bat"), path.join(finalOutputPath, "/install.bat"));

	console.log(`Build done`);
}

build();
