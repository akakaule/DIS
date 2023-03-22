/* @jsx jsx */
import { Badge, Box, Flex, Grid, Heading } from "@chakra-ui/react";
import { jsx } from "@emotion/react";
import { EndpointStatusCount, MetadataShort } from "api-client";
import Loading from "components/loading/loading";
import { HeartbeatStatus } from "functions/endpoint.functions";
import { getEnv } from "hooks/app-status";
import { useTime } from "hooks/use-time";
import React from "react";
import EndpointStatusCard from "./endpoint-status-card/endpoint-status-card";

type DashboardProps = {
  title: string;
  cards: EndpointStatusCount[];
  heartbeatDict: MetadataShort[];
};

const Dashboard = <T extends object>(props: DashboardProps) => {
  const loading = !props.cards;
  const time = useTime(1000);
  const [env, setEnv] = React.useState(getEnv());
  return (
    <Box padding="5rem" bg="gray.700" color="gray.100" height="100%">
      <Box display="flex" justifyContent="space-between" paddingBottom="2rem">
        <Heading>
          {props.title} <Badge color="blue.900">{env !== undefined ? env : getEnv()}</Badge>
        </Heading>
        <Heading>
          <span>{time.format("HH:mm:ss")}</span>
        </Heading>
      </Box>
      {loading ? (
        <Flex justifyContent="center" alignItems="center" marginTop="40%">
          <Loading diameter={100} />
        </Flex>
      ) : (
        <Grid alignItems="center" autoFlow="row" gridAutoFlow="row" templateColumns="repeat(auto-fill, minmax(320px, 1fr))" gap={6}>
          {props.cards?.map((x) => {
            const heartbeatDict = props.heartbeatDict ? props.heartbeatDict[props.heartbeatDict.findIndex((e) => e.endpointId === x.endpointId)] : undefined;
            const heartbeatStatus = heartbeatDict ? HeartbeatStatus[heartbeatDict.heartbeatStatus as keyof typeof HeartbeatStatus] : HeartbeatStatus.Unknown;

            return <EndpointStatusCard statusCount={x} heartbeatStatus={heartbeatStatus} key={x.endpointId} />;
          })}
        </Grid>
      )}
    </Box>
  );
};

export default Dashboard;
