import App from "./app";
import { BrowserRouter } from "react-router-dom";
import React from "react";
import { hydrate } from "react-dom";
import { ChakraProvider, ColorModeProvider, CSSReset } from "@chakra-ui/react";
import theme from "shared-styles/theme";

const props = JSON.parse((window as any).__INITIAL_STATE__);

hydrate(
  <BrowserRouter>
    <ChakraProvider theme={theme}>
      <CSSReset />
      <ColorModeProvider>
        <App {...props} />
      </ColorModeProvider>
    </ChakraProvider>
  </BrowserRouter>,
  document.getElementById("root")
);

if (module?.hot) {
  module.hot.accept();
}
