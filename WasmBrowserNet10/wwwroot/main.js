import { dotnet } from './_framework/dotnet.js'

globalThis.getBaseUri = () => {
    return window.location.origin + window.location.pathname.replace(/[^/]*$/, '');
};

const { runMain } = await dotnet
    .withApplicationArguments("start")
    .create();

await runMain();
