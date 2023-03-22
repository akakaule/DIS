import * as React from "react";
import * as api from "api-client";
import Table from "@material-ui/core/Table";
import TableBody from "@material-ui/core/TableBody";
import TableCell from "@material-ui/core/TableCell";
import TableRow from "@material-ui/core/TableRow";
import { Box, Badge, Collapse, Code } from "@chakra-ui/react";
import Button from "components/button";
import { formatMoment } from "functions/endpoint.functions";
interface ILogListingProps {
  logs: api.EventLogEntry[];
}

export default function LogListing(props: ILogListingProps) {
  return (
    <Box width="100%">
      {props.logs?.map((l) => {
        const [show, setShow] = React.useState(false);
        const handleToggle = () => setShow(!show);
        return (
          <Box key="" borderWidth="1px" rounded="lg">
            <Badge>{l.messageType}</Badge>{" "}
            {formatMoment(l?.timeStamp)} by <b>{l.from}</b>
            <p>{l.text}</p>
            <br/>
            <Collapse in={show}>
              <Table size="small" aria-label="a dense table">
                <TableBody>
                  <TableRow hover={true}>
                    <TableCell>
                      <b>EventType</b>
                    </TableCell>
                    <TableCell>{l.eventType}</TableCell>
                  </TableRow>
                  <TableRow hover={true}>
                    <TableCell>
                      <b>EventId</b>
                    </TableCell>
                    <TableCell>{l.eventId}</TableCell>
                  </TableRow>
                  <TableRow hover={true}>
                    <TableCell>
                      <b>SessionId</b>
                    </TableCell>
                    <TableCell>{l.sessionId}</TableCell>
                  </TableRow>
                  <TableRow hover={true}>
                    <TableCell>
                      <b>MessageId</b>
                    </TableCell>
                    <TableCell>{l.messageId}</TableCell>
                  </TableRow>
                  <TableRow hover={true}>
                    <TableCell>
                      <b>CorelationId</b>
                    </TableCell>
                    <TableCell>{l.correlationId}</TableCell>
                  </TableRow>
                  <TableRow hover={true}>
                    <TableCell>
                      <b>Published By</b>
                    </TableCell>
                    <TableCell>{l.from}</TableCell>
                  </TableRow>
                  <TableRow hover={true}>
                    <TableCell>
                      <b>Payload</b>
                    </TableCell>
                    <TableCell>
                      <Code>{l.payload}</Code>
                    </TableCell>
                  </TableRow>
                  <TableRow hover={true}>
                    <TableCell>
                      <b>MessageType</b>
                    </TableCell>
                    <TableCell>{l.messageType}</TableCell>
                  </TableRow>
                  <TableRow hover={true}>
                    <TableCell>
                      <b>Is Deferred</b>
                    </TableCell>
                    <TableCell>{l.isDeferred}</TableCell>
                  </TableRow>
                </TableBody>
              </Table>
            </Collapse>
            <br/>
            <Button size="xs" onClick={handleToggle}>
              {show ? "Hide Details" : "Show Details"}
            </Button>
            {/* <p>{l.} by <a href={`/Endpoints/Details/${h.from}`}>{h.from}</a></p>
                    <br></br>
                    <p><b>Exception</b></p>
                    <p>{h.errorContent?.exceptionStackTrace}</p>
                    <br></br>
                    <p><b>To</b></p>
                    <p>{h.to}</p>
                    <br></br> */}
          </Box>
        );
      })}
    </Box>
  );
}
