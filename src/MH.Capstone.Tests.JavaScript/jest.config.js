/** @type {import('jest').Config} */
module.exports = {
    // Use jsdom to simulate a browser environment (window, document, etc.)
    testEnvironment: 'jest-environment-jsdom',

    // Find tests in __tests__/ directories only
    testMatch: ['**/__tests__/**/*.test.js'],

    // Force Jest to exit after all tests complete, even if there are open handles
    // (e.g., unclosed IndexedDB connections from fake-indexeddb).
    forceExit: true,

    // Polyfill structuredClone for jest-environment-jsdom, which uses jsdom@20
    // and does not expose structuredClone on the global. fake-indexeddb@6 requires it.
    setupFiles: ['./jest.setup.js'],

    // Source files to include in coverage reports
    collectCoverageFrom: [
        '<rootDir>/../MH.Capstone.WebApp/wwwroot/js/**/*.js',
        '!<rootDir>/../MH.Capstone.WebApp/wwwroot/js/lib/**'
    ],

    coverageDirectory: '<rootDir>/coverage',

    coverageReporters: ['text', 'lcov', 'html'],

    // Fail if coverage drops below these thresholds (raise as test suite matures)
    coverageThreshold: {
        global: {
            branches: 60,
            functions: 60,
            lines: 60,
            statements: 60
        }
    }
};
