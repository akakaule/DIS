import * as React from "react";
import * as api from "api-client";
import { Box, Heading } from "@chakra-ui/react";
import { formatMoment } from "functions/endpoint.functions";
import { makeStyles } from "@material-ui/core";
interface IHistoryListingProps {
  histories: api.Message[];
}

export default function HistoryListings(props: IHistoryListingProps) {
  return (
    <Box width="100%" marginRight="1rem" display="flex" flexDirection="column">
      <Box overflow="auto" flex="1">
      {props.histories.map((h) => {
        return (
          <Box key="" borderWidth="1px" rounded="lg" marginBottom="1rem" padding="1rem">
            <Heading as="h4" size="md">
              {h.messageType}
            </Heading>
            <p>
              {formatMoment(h.enqueuedTimeUtc)}              
            </p>
            <br/>
            <p>
              <b>{h.errorContent?.exceptionStackTrace != undefined ? "Exception" : ""}</b>
            </p>
            <p>{h.errorContent?.exceptionStackTrace}</p>
            <br/>
            <p>
              <b>{h.eventContent != undefined ? "Payload" : ""}</b>
            </p>
            <pre>{h.eventContent != undefined ? JSON.stringify(JSON.parse(h.eventContent),null,2) : ""}</pre>
            <br/>
            <p>
                <b>From:</b> {h.from}
            </p>
            <br/>
            <p>
                <b>To:</b> {h.to}
            </p>
            <br/>
          </Box>
        );
      })}
      </Box>
    </Box>
  );
}
