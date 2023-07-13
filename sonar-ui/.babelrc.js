const jsx = require('@babel/plugin-transform-react-jsx');
const pragmatic = require('@emotion/babel-plugin-jsx-pragmatic');
const emotion = require('@emotion/babel-plugin');

const pragmaName = '___EmotionJSX';

// NOTE: this is not used as part of the normal build. It is only for testing
// purposes because Jest bypasses react-app-rewired.
// NOTE: this code is based on https://emotion.sh/docs/@emotion/babel-preset-css-prop
// unfortunately that preset didn't work when applied via package.json but it
// works here.
module.exports = function (api) {
  api.cache(true);

  return {
    presets: [],
    plugins: [
      [
        pragmatic,
        {
          export: 'jsx',
          module: '@emotion/react',
          import: pragmaName
        }
      ],
      [
        jsx,
        {
          pragma: pragmaName,
          pragmaFrag: 'React.Fragment'
        }
      ],
      [
        emotion,
        {
          cssPropOptimization: true
        }
      ]
    ]
  };
};
