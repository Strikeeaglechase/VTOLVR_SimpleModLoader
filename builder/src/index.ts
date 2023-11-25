import { execSync } from "child_process";
import fs from "fs";
import path from "path";

const base = "../..";
const csprojPaths: string[] = [`${base}/IMLLoader`, `${base}/ModLoaderPolyfill`, `${base}/SMLInstaller`];

const outputPath = `${base}/SimpleModLoaderInstaller`;

function buildCsProjects() {
	for (const csprojPath of csprojPaths) {
		console.log(`Building ${csprojPath}`);
		execSync("dotnet build", { cwd: path.resolve(csprojPath) });
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

function build() {
	if (fs.existsSync(outputPath)) {
		fs.rmSync(outputPath, { recursive: true });
	}
	fs.mkdirSync(outputPath);

	buildCsProjects();

	// Copy asset files
	rcopy(`${base}/SMLInstaller/assets`, `${outputPath}/assets`);
	rcopy(`${base}/SMLInstaller/root_assets`, `${outputPath}/root_assets`);

	// Copy in subproject DLLs
	fs.copyFileSync(`${base}/IMLLoader/bin/Debug/netstandard2.0/IMLLoader.dll`, `${outputPath}/IMLLoader.dll`);
	fs.copyFileSync(`${base}/ModLoaderPolyfill/bin/Debug/netstandard2.0/ModLoaderPolyfill.dll`, `${outputPath}/ModLoaderPolyfill.dll`);

	// Copy installer files
	const files = fs.readdirSync(`${base}/SMLInstaller/bin/Debug/net5.0`);
	for (const file of files) {
		if (file.endsWith(".dev.json") || file.endsWith(".pdb")) continue;
		const outFileName = file; // == "SMLInstaller.exe" ? "SimpleModLoaderInstaller.exe" : file;
		fs.copyFileSync(`${base}/SMLInstaller/bin/Debug/net5.0/${file}`, `${outputPath}/${outFileName}`);
	}

	// Copy installer batch file
	fs.copyFileSync(`../install_RUN_ME.bat`, `${outputPath}/install_RUN_ME.bat`);

	console.log(`Build done`);
}

build();
