import App from "./app";
import React from "react";
import { StaticRouter } from "react-router-dom/server";
import { renderToString } from "react-dom/server";
import { ChakraProvider, ColorModeProvider, CSSReset } from "@chakra-ui/react";
import theme from "shared-styles/theme";

const assets = require(process.env.RAZZLE_ASSETS_MANIFEST!);

const server = (cb: any, url: string, props: { [K: string]: any }) => {
  const markup = renderToString(
    <StaticRouter location={url}>
      <ChakraProvider theme={theme}>
        <CSSReset />
        <ColorModeProvider>
          <App {...props} />
        </ColorModeProvider>
      </ChakraProvider>
    </StaticRouter>
  );

  cb(
    null,
    `<!doctype html>
      <html lang="">
      <head>
          <meta http-equiv="X-UA-Compatible" content="IE=edge" />
          <meta charset="utf-8" />
          <title>ESB Management</title>
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <link rel="stylesheet" href="https://fonts.googleapis.com/icon?family=Material+Icons">
          ${
            assets.client.css
              ? `<link rel="stylesheet" href="${assets.client.css}">`
              : ""
          } 
          ${
            process.env.NODE_ENV === "production"
              ? `<script src="${assets.client.js}" defer></script>`
              : `<script src="${assets.client.js}" defer crossorigin></script>`
          }
      </head>
      <body>
          <div id="root">${markup}</div>
      </body>
      <script>
          window.__INITIAL_STATE__ = '${JSON.stringify(props)}';
      </script>
    </html>`
  );
};

if (module?.hot) {
  module.hot.accept();
}

export default server;
