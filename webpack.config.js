const webpack = require("webpack")
const path = require('path')
// const Offline = require('offline-plugin')
const OptimizeJsPlugin = require("optimize-js-plugin")
const Extract = require('extract-text-webpack-plugin')

// debug?
const isDebug = process.env.NODE_ENV !== "production"

// load and process styles
const CSS_LOADERS = [
  `css-loader?${JSON.stringify({
    sourceMap: isDebug,
    modules: false,
    importLoaders: 1,
    localIdentName: isDebug ? '[name]_[local]_[hash:base64:3]' : '[hash:base64:4]',
    minimize: !isDebug,
  })}`,
  'postcss-loader',
]

const extract_css = new Extract({ filename: 'style.css', allChunks: true })

// Webpack configuration
// http://webpack.github.io/docs/configuration.html
let u = {

  // Developer tool to enhance debugging, source maps
  // http://webpack.github.io/docs/configuration.html#devtool
  devtool: isDebug ? 'inline-source-map' : false,

  // Options affecting the normal modules
  // https://webpack.github.io/docs/list-of-loaders.html
  module: {
    loaders: [
      {
        exclude: /node_modules/,
        loader: "babel-loader",
        query: { cacheDirectory: true, compact: true },
        test: /\.(jsx|js|es|es6|mjs)$/,
      }
    ]
  },

  // http://webpack.github.io/docs/configuration.html#resolve
  resolve: {
    alias: {
      // components: path.resolve('src/components/'),
    }
  },

  node: {
    __filename: true,
    __dirname: true,
  },

  output: {
        devtoolModuleFilenameTemplate: "[absolute-resource-path]",
        filename: "[name].js",
        chunkFilename: "[id].js",
        sourceMapFilename: "[name].js.map",
        path: path.resolve(__dirname, './assets/build'),
        publicPath: '/',
    },
}

const defaultPlugins = () => [
    new webpack.NoErrorsPlugin,
    new webpack.LoaderOptionsPlugin({
        minimize: isDebug,
        debug: isDebug,
        options: {
            context: __dirname,
            postcss: [
                require('precss'),
                require('postcss-calc')(),
                require('pleeease-filters')(),
                require('pixrem')(),
                require('autoprefixer')(),
            ]
        }
    }),
    new webpack.NamedModulesPlugin(),
    new webpack.ProvidePlugin({
      React: 'react',
      DOM: 'react-dom',
      ReactDOM: 'react-dom',
      utils: 'universal-utils',
    }),
    new OptimizeJsPlugin({sourceMap: false})
    // new Offline({
    //   ServiceWorker: {
    //     events: true,
    //     caches: {
    //       main: ['/', '/**.js'],
    //     }
    //   }
    // }),
  ]//.concat(isDebug ? [new webpack.HotModuleReplacementPlugin] : [])

let configs = [
  // browser
  {
    entry: {
      "app.min": ["./assets/js/app.js"],
    },
    target: "web",
    plugins: defaultPlugins().concat([extract_css]),
    module: {
      loaders: u.module.loaders.concat([
        {
          test: /\.(scss|sass|css)/, // load sass, scss, or css files and compile
          exclude: /node_modules/,   // find modules NOT imported from node_modules
          loaders: extract_css.extract(CSS_LOADERS)
        },
        {
          test: /\.(scss|sass|css)/, // load sass, scss, or css files and compile
          include: /node_modules/,   // find files imported from node_modules
          loader: extract_css.extract('css?sourceMap&minimize')
        },
        {
          test: /\.(png|jpg|jpeg|gif|svg|woff|woff2)$/,
          loader: 'url-loader?limit=100000',
        },
      ])
    },
  },
]

configs = configs.map(x => Object.assign({}, u, x))

// Optimize the bundle in release (production) mode
const productionify = (c => {
  if(!isDebug){
    // In Webpack 2: the occurrence order plugin is on by default and is no longer needed
    // See https://gist.github.com/sokra/27b24881210b56bbaff7#occurrence-order for more info
    c.plugins.push(new webpack.optimize.UglifyJsPlugin({ compress: { warnings: false } }))
    // c.plugins.push(new OptimizeJsPlugin({sourceMap: false}))
    c.plugins.push(new webpack.optimize.AggressiveMergingPlugin)
  }
  return c
})

configs[0] = productionify(configs[0])

module.exports = {default: configs[0], isDebug }
