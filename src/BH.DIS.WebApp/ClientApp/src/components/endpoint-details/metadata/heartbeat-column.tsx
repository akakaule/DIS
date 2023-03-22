import { QuestionIcon, TimeIcon } from "@chakra-ui/icons";
import { Box, Center, Icon, SimpleGrid, Table, TableCaption, TableContainer, Tbody, Td, Text, Tfoot, Th, Thead, Tooltip, Tr } from "@chakra-ui/react";
import * as api from "api-client";
import { formatMoment } from "functions/endpoint.functions";
import React from "react";
import "./metadata.css";

interface IMetadataolumnProps {
  metadata: api.Metadata | undefined;
}

const HeartbeatColumn = (props: IMetadataolumnProps) => {
  const [endpointMetadata, setendpointMetadata] = React.useState<api.Metadata | undefined>(props.metadata);
  const [heartbeatActive, setHeartbeatActive] = React.useState<boolean>(true);
  const [heartbeatDataAviable, setHeartbeatDataAviable] = React.useState<boolean>(false);

  React.useEffect(() => {
    if (endpointMetadata?.heartBeats && endpointMetadata.heartBeats.length !== 0) {
      setHeartbeatDataAviable(true);
    } else {
      setHeartbeatDataAviable(false);
    }
  }, [endpointMetadata]);

  const On = (scale: number, margin: string, top: number) => {
    let height = 71.5 * scale;
    return (
      <Box className="heart-rate" margin={margin} height={height + "px"} top={top}>
        <svg
          version="1.0"
          xmlns="http://www.w3.org/2000/svg"
          xmlnsXlink="http://www.w3.org/1999/xlink"
          x="0px"
          y="0px"
          width="75px"
          height="36.5px"
          viewBox="0 0 75 36.5"
          enableBackground="new 0 0 75 36.5"
          xmlSpace="preserve"
          transform={"scale(" + scale + ")"}
        >
          <polyline
            fill="none"
            stroke="#c9201a"
            strokeWidth="3"
            strokeMiterlimit="10"
            points="0,22.743 19.257,22.743 22.2975,16.662 25.338,22.743 28.8855 ,22.743 31.419,27.811 35.9795,4.5 40.0335,31.8645 42.0611,22.743 48.6485,22.743 51.6895,20.2095  55.2365 ,22.743 75,22.743"
          />
        </svg>
        <Box className="fade-in" height={height + "px"}></Box>
        <Box className="fade-out" height={height + "px"}></Box>
      </Box>
    );
  };

  const Off = (scale: number, margin: string, top: number) => {
    let height = 71.5 * scale;

    return (
      <Box className="heart-rate" margin={margin} height={height + "px"} top={top}>
        <svg
          version="1.0"
          xmlns="http://www.w3.org/2000/svg"
          xmlnsXlink="http://www.w3.org/1999/xlink"
          x="0px"
          y="0px"
          width="75px"
          height="36.5px"
          viewBox="0 0 75 36.5"
          enableBackground="new 0 0 75 36.5"
          xmlSpace="preserve"
          transform={"scale(" + scale + ")"}
        >
          <polyline
            fill="none"
            stroke="#c9201a"
            strokeWidth="3"
            strokeMiterlimit="10"
            points="0,22.743 19.257,22.743 22.2975,22.743 25.338,22.743 28.8855 ,22.743 31.419,22.743 35.9795,22.743 40.0335,22.743 42.0611,22.743 48.6485,22.743 51.6895,22.743  55.2365 ,22.743 75,22.743"
          />
        </svg>
        <Box className="fade-in" height={height + "px"}></Box>
        <Box className="fade-out" height={height + "px"}></Box>
      </Box>
    );
  };

  const Unknown = () => (
    <Center>
      <QuestionIcon boxSize={"2em"} />
    </Center>
  );

  const Pending = () => (
    <Center>
      <TimeIcon boxSize={"2em"} />
    </Center>
  );

  function heartbeatStatus(status: string) {
    switch (status) {
      case "On":
        return On(1.2, "0px auto", 5);
      case "Off":
        return Off(1.2, "0px auto", 5);
      case "Unknown":
        return <Unknown />;
      case "Pending":
        return <Pending />;
      default:
        return <Unknown />;
    }
  }

  function heartbeatStatusTable(status: string) {
    switch (status) {
      case "On":
        return On(0.75, "0px 0px 0px -20px", 0);
      case "Off":
        return Off(0.75, "0px 0px 0px -20px", 0);
      case "Unknown":
        return <Unknown />;
      case "Pending":
        return <Pending />;
      default:
        return <Unknown />;
    }
  }

  const HasHeartbeatData = () => (
    <Box>
      <Text fontWeight={"bold"} textAlign={"center"}>
        Heartbeat
      </Text>
      <Tooltip label={"The current heartbeat status of " + endpointMetadata?.id + " is " + endpointMetadata?.endpointHeartbeatStatus!}>
        <Box padding="3">{heartbeatStatus(endpointMetadata?.endpointHeartbeatStatus!)}</Box>
      </Tooltip>
      <TableContainer>
        <Table variant="simple" size={"sm"}>
          <Thead>
            <Tr>
              <Th>Start</Th>
              <Th>Received</Th>
              <Th>Queue (ms)</Th>
              <Th>Status</Th>
            </Tr>
          </Thead>
          <Tbody>
            {endpointMetadata?.heartBeats?.map((heartbeat, idx) => (
              <Tr key={idx + "row"}>
                <Td key={idx + "start"}>{formatMoment(heartbeat.startTime, true)}</Td>
                <Td key={idx + "rec"}>{formatMoment(heartbeat.receivedTime, true)}</Td>
                <Td key={idx + "que"}>{heartbeat.endTime?.diff(heartbeat.startTime)}</Td>
                <Td key={idx + "status"}>{heartbeatStatusTable(heartbeat.endpointHeartbeatStatus!)}</Td>
              </Tr>
            ))}
          </Tbody>
        </Table>
      </TableContainer>
    </Box>
  );

  const NoHeartbeatData = () => (
    <Box>
      <Text fontWeight={"bold"} textAlign={"center"}>
        Heartbeat
      </Text>
      <Box padding="3">
        <Text textAlign={"center"}>There is no heartbeat data available</Text>
      </Box>
    </Box>
  );

  return heartbeatDataAviable ? <HasHeartbeatData /> : <NoHeartbeatData />;
};

export default HeartbeatColumn;
