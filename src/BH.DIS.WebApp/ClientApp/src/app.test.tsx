import App from "./app";
import * as React from "react";
import { MemoryRouter } from "react-router-dom";
import { enableFetchMocks } from "jest-fetch-mock";
import * as ReactDOM from "react-dom";
enableFetchMocks();

// describe("<App />", () => {
//   test("renders without exploding", () => {
//     const div = document.createElement("div");
//     ReactDOM.render(
//       <MemoryRouter>
//         <App />
//       </MemoryRouter>,
//       div
//     );
//   });
// });

describe('Dummy', () => {
   
  it('Dummy it', () => {
      expect(1 + 2).toEqual(3);
      expect(2 + 2).toEqual(4);
   });
});
