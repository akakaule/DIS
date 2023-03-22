/* @jsx jsx */
import * as React from "react";
import { Route, Routes, useLocation } from "react-router-dom";
import Monitor from "pages/monitor";
import { Flex } from "@chakra-ui/react";
import Header from "components/header";
import { Global, jsx, css } from "@emotion/react";
import EndpointDetails from "pages/endpoint-details";
import EventDetails from "pages/event-details";
import EndpointsList from "pages/endpoints-list";
import Footer from "components/footer";
import { Navigation } from "models/navigation";
const liveMonitorRouteName = "Live Monitor";
const eventTypesName = "Event Types";

// components with renderProp are included in the client-side routing
// components without renderProp are treated as external routes (server-side routing)
const navigation: Navigation = [
  {
    name: "EndpointDetails",
    path: "/Endpoints/Details/:id",
    header: false,
    render: (serverProps: any) => (clientProps: any) => <EndpointDetails {...serverProps} {...clientProps} />,
  },
  {
    name: "EventDetails",
    path: "/Message/Index/:endpointId/:id",
    header: false,
    render: (serverProps: any) => (clientProps: any) => <EventDetails {...serverProps} {...clientProps} />,
  },
  {
    name: "EventDetails",
    path: "/Message/Index/:endpointId/:id/:backindex",
    header: false,
    render: (serverProps: any) => (clientProps: any) => <EventDetails {...serverProps} {...clientProps} />,
  },
  {
    name: "Endpoints",
    path: "/Endpoints",
    header: true,
    render: (serverProps: any) => (clientProps: any) => <EndpointsList {...serverProps} {...clientProps} />,
  },
  { name: eventTypesName, path: "/EventTypes", header: true },
  {
    name: liveMonitorRouteName,
    path: "/Monitor",
    header: true,
    render: (serverProps: any) => (clientProps: any) => <Monitor {...serverProps} {...clientProps} />,
  },
  {
    name: "Front",
    path: "/",
    header: false,
    render: (serverProps: any) => (clientProps: any) => <EndpointsList {...serverProps} {...clientProps} />,
  },
];

const App = (serverProps: { [K: string]: any }) => (
  <Flex height="100vh" direction="column">
    <Global
      styles={css`
        body {
          margin: 0;
          padding: 0;
        }
        * {
          box-sizing: border-box;
        }
      `}
    />

    {routeIsLiveMonitor() ? undefined : <Header links={navigation.filter((x) => x.header)} />}

    <Routes>
      {getReactRoutes().map((x) => (
        <Route path={x.path} key={x.path} element={x.render!(serverProps)(serverProps)} />
      ))}
    </Routes>

    {routeIsLiveMonitor() || routeIsEventTypes() ? undefined : <Footer />}
  </Flex>
);

const getReactRoutes = (): Navigation => {
  return navigation.filter((x) => x.render);
};

const routeIsLiveMonitor = (): boolean => {
  return navigation.filter((x) => x.name === liveMonitorRouteName)[0].path === useLocation().pathname;
};

const routeIsEventTypes = (): boolean => {
  return navigation.filter((x) => x.name === eventTypesName)[0].path === useLocation().pathname;
};

export default App;
