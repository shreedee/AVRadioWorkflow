const path = require('path');
const merge = require('webpack-merge');
const MiniCssExtractPlugin = require("mini-css-extract-plugin");
const HardSourceWebpackPlugin = require('hard-source-webpack-plugin');

const env = process.env.NODE_ENV;

module.exports = (env, argv) => {

    console.log('mode is' + argv.mode);

    const isDevBuild = argv.mode === 'development';


    let sharedConfig = {
        resolve: {
            extensions: ['.tsx', '.ts', '.js'],
            modules: [
                path.resolve(__dirname),
                'node_modules'
            ]
        },
        output: {
            filename: '[name].js',
            chunkFilename: '[id].[chunkhash].js',
            publicPath: 'dist/' // Webpack dev middleware, if enabled, handles requests for this URL prefix
        },
        module: {
            rules: [
                {
                    test: /\.tsx?$/,
                    use: 'ts-loader',
                    exclude: /node_modules/,
                }
            ],
        }
        
    };

    if (isDevBuild) {
        console.log("Doing DEV build");

        sharedConfig = merge(sharedConfig, {
            devtool: 'inline-source-map',
            plugins: [
                new HardSourceWebpackPlugin()
            ]
        });
    }
    else
        console.log("Doing Production build");


    const clientBundleOutputDir = './wwwroot/dist';
    const clientBundleConfig = merge(sharedConfig, {
        entry: { 'main-client': './ClientApp/bootCommon/boot-client.tsx' },
        output: { path: path.join(__dirname, clientBundleOutputDir) },
        module: {
            rules: [
                {
                    test: /\.s?[ac]ss$/,
                    use: [
                        MiniCssExtractPlugin.loader,
                        { loader: 'css-loader', options: { url: false, sourceMap: true } },
                        { loader: 'sass-loader', options: { sourceMap: true } }
                    ],
                },
                { test: /\.(png|jpg|jpeg|gif|svg)$/, use: 'url-loader?limit=25000' },
                { test: /\.(woff2|woff|ttf|eot|svg)(\?v=[a-z0-9]\.[a-z0-9]\.[a-z0-9])?$/, use: 'url-loader?limit=25000' }

            ],
        },
        plugins: [
            new MiniCssExtractPlugin({
                filename: "site.css",
                chunkFilename: '[id].[chunkhash].css',
            })
        ],

    });

    const serverBundleConfig = merge(sharedConfig, {
        entry: { 'main-server': './ClientApp/bootCommon/boot-server.tsx' },
        module: {
            rules: [
                {
                    test: /\.s?[ac]ss$/,
                    use: 'null-loader',
                },
                { test: /\.(png|jpg|jpeg|gif|svg)$/, use: 'null-loader' },
                { test: /\.(woff2|woff|ttf|eot|svg)(\?v=[a-z0-9]\.[a-z0-9]\.[a-z0-9])?$/, use: 'null-loader' },
                { test: /\.clientOnly.ts/, loaders: ['ts-loader', './ClientApp/bootCommon/no-loader'] }

            ]
        },
        output: {
            libraryTarget: 'commonjs',
            path: path.join(__dirname, './ClientApp/preRenderDist')
        },
        target: 'node',
        devtool: 'inline-source-map'
    });

    return [clientBundleConfig, serverBundleConfig];
}