// Punto de entrada de CDK: `npx cdk synth --all -c env=dev|prod` (despliega el dueño; ver README.md).
import { App } from 'aws-cdk-lib';
import { buildApp } from '../lib/app-builder.ts';
import { loadConfig } from '../lib/config.ts';

const app = new App();
buildApp(app, loadConfig(app.node));
app.synth();
