/** @type {import('jest').Config} */
module.exports = {
    // Use jsdom to simulate a browser environment (window, document, etc.)
    testEnvironment: 'jest-environment-jsdom',

    // Find tests in __tests__/ directories only
    testMatch: ['**/__tests__/**/*.test.js'],

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
