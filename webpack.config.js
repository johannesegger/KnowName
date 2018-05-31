var path = require("path");
var webpack = require("webpack");
var fableUtils = require("fable-utils");

function resolve(filePath) {
  return path.join(__dirname, filePath)
}

var babelOptions = fableUtils.resolveBabelOptions({
  presets: [
    ["env", {
      "targets": {
        "browsers": ["last 2 versions"]
      },
      "modules": false
    }]
  ],
  plugins: ["transform-runtime"]
});

var isProduction = process.argv.indexOf("-p") >= 0;
var port = process.env.SUAVE_FABLE_PORT || "8085";
console.log("Bundling for " + (isProduction ? "production" : "development") + "...");

module.exports = {
  devtool: "source-map",
  entry: {
    client: resolve('./src/Client/Client.fsproj'),
    sw: resolve('./src/ServiceWorker/ServiceWorker.fsproj')
  },
  output: {
    path: resolve('./public'),
    publicPath: "/public",
    filename: "[name].js"
  },
  resolve: {
    modules: [ resolve("./node_modules/")]
  },
  devServer: {
    host: '0.0.0.0',
    port: 8080,
    https: true,
    proxy: {
      '/api/*': {
        target: 'http://localhost:' + port,
        changeOrigin: true
      }
    },
    hot: true,
    inline: true
  },
  module: {
    rules: [
      {
        test: /\.fs(x|proj)?$/,
        use: {
          loader: "fable-loader",
          options: {
            babel: babelOptions,
            define: isProduction ? [] : ["DEBUG"]
          }
        }
      },
      {
        test: /\.js$/,
        exclude: /node_modules/,
        use: {
          loader: 'babel-loader',
          options: babelOptions
        },
      },
      {
        test: /\.s(a|c)ss$/,
        use: [
          "style-loader",
          "css-loader",
          "sass-loader"
        ]
      },
      {
        test: /\.(eot|svg|ttf|woff|woff2|webmanifest)(\?|$)/,
        use: {
          loader: 'file-loader',
          options: {
            name: '[path][name].[ext]'
          }
        }
      }
    ]
  },
  plugins : isProduction ? [] : [
      new webpack.HotModuleReplacementPlugin(),
      new webpack.NamedModulesPlugin()
  ]
};
