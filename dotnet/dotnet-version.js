const fs = require('fs');

const yargsParser = require('yargs-parser');
const parser = require('xml2json');

const COMMAND_HANDLERS = {
  'set-package-version': setPackageVersion,
  'get-package-version': getPackageVersion
};

function getPackageVersion() {
  const version = JSON.parse(getJsonFromFile()).Project.PropertyGroup.Version;
  console.log(version);

  return version;
}

function setPackageVersion(version) {
  const csProj = fs.readFileSync('./src/ProcessEngine.ExternalTaskAPI.Client.csproj', {encoding: 'utf8'});
  const csProjWithNewVersion = csProj.replace(getPackageVersion().toString(), version);
  fs.writeFileSync('./src/ProcessEngine.ExternalTaskAPI.Client.csproj', csProjWithNewVersion);
  console.log(`Version set to ${version}`);
}

function getJsonFromFile() {
  const csproj = fs.readFileSync('./src/ProcessEngine.ExternalTaskAPI.Client.csproj', {encoding: 'utf8'});

  const json = parser.toJson(csproj);

  return json;
}

function run(originalArgv) {
  const [, , ...args] = originalArgv;
  yargsParser(args);

  const [commandName, restArgs] = args;

  COMMAND_HANDLERS[commandName](restArgs);
}

run(process.argv);
