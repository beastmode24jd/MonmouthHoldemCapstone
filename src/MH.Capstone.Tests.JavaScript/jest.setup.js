// jest.setup.js — runs before each test file via jest.config.js setupFiles.
// jest-environment-jsdom (v29) uses jsdom@20 which does not expose structuredClone
// on the global object. fake-indexeddb@6 calls structuredClone when persisting values,
// so we polyfill it here before any test modules are loaded.
if (typeof globalThis.structuredClone === 'undefined') {
    globalThis.structuredClone = function structuredClone(val) {
        return JSON.parse(JSON.stringify(val));
    };
}
