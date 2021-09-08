
const path = require("path");
module.exports = [{
    entry: `./public/unity.ts`,
    target: "web",
    mode: "production",
    module: {
        rules: [{
            test: /\.tsx?$/,
            loader: "ts-loader",
            exclude: /node_modules/,
            options: {
                configFile: `tsconfig.json`
            }
        }]
    },
    resolve: {
        extensions: [".tsx", ".ts", ".js"]
    },
    output: {
        filename: "unity.js",
        path: path.resolve(__dirname, "public/js"),
    },
},
{
    entry: `./public/adminUnity.ts`,
    target: "web",
    mode: "production",
    module: {
        rules: [{
            test: /\.tsx?$/,
            loader: "ts-loader",
            exclude: /node_modules/,
            options: {
                configFile: `tsconfig.json`
            }
        }]
    },
    resolve: {
        extensions: [".tsx", ".ts", ".js"]
    },
    output: {
        filename: "adminUnity.js",
        path: path.resolve(__dirname, "public/js"),
    },
}];