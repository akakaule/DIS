import App from "./app";
import { BrowserRouter } from "react-router-dom";
import React from "react";
import * as ReactDOM from "react-dom";
import { ChakraProvider, ColorModeProvider, CSSReset } from "@chakra-ui/react";
import theme from "shared-styles/theme";
import { createRoot } from 'react-dom/client';

const rootElement = document.getElementById("root");
const root = createRoot(rootElement!);

root.render(
  <BrowserRouter>
    <ChakraProvider theme={theme}>
      <CSSReset />
      <ColorModeProvider>
        <App />
      </ColorModeProvider>
    </ChakraProvider>
  </BrowserRouter>
)

if (module?.hot) {
  module.hot.accept();
}
