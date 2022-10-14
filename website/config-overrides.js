// https://github.com/timarney/react-app-rewired
module.exports = {
    /**
     *
     * @param {import("webpack").Configuration} webpackConfig
     * @param {*} env
     * @returns {import("webpack").Configuration}
     */
    webpack: (webpackConfig, env) => {
        /* Any webpack configuration options: https://webpack.js.org/configuration */
        webpackConfig.optimization.splitChunks = {
            chunks: "all",
            minRemainingSize: 0,
            minChunks: 1,
            maxAsyncRequests: 10,
            maxInitialRequests: 10,
            cacheGroups: {
                defaultVendors: {
                    test: /[\\/]node_modules[\\/]/,
                    priority: -10,
                    reuseExistingChunk: true,
                },
                default: {
                    minChunks: 2,
                    priority: -20,
                    reuseExistingChunk: true,
                },
                react: {
                    test: /[\\/]node_modules[\\/](react|react-dom)[\\/]/,
                    name: "react",
                    chunks: "all",
                },
            },
        };
        return webpackConfig;
    },
};
