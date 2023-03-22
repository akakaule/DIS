import * as React from "react";
import { Box, Heading, Code, Textarea } from "@chakra-ui/react";
import { formatMoment } from "functions/endpoint.functions";
import { PendingEvent } from "pages/event-details";
import TablePagination from "@material-ui/core/TablePagination";

interface IPendingListing {
  events: PendingEvent[];
  onPageChange?: () => void;
}

export default function BlockedListing(props: IPendingListing) {
  const count = props.events.length ?? 0;
  const [page, setPage] = React.useState(0);
  const [rowsPerPage, setRowsPerPage] = React.useState(6); // Events pr. page
  const onPageChange = props.onPageChange;

  const handleChangePage = (event: any, newPage: any) => {
    setPage(newPage);
    if (onPageChange !== undefined) {
      onPageChange();
    }
  };

  const handleChangeRowsPerPage = (event: any) => {
    setRowsPerPage(parseInt(event.target.value, 10));
    setPage(0);
  };

  function formatPayload(payload: string): string {
    const object: any = {};
    // JSON.parse(payload).forEach((e:any) => {
    //   console.log(e)
    //   if (e.name != null) {
    //     object[e.name] = e.typeName;
    //   }
    // });

    return JSON.stringify(JSON.parse(payload), null, 2);
  }
  return (
    <Box width="100%" marginRight="1rem" display="flex" flexDirection="column">
      <Box overflow="auto" flex="1">
        {props.events
          .sort((a, b) => (a.message.enqueuedTimeUtc! > b.message.enqueuedTimeUtc! ? 1 : -1))
          .slice(page * rowsPerPage, page * rowsPerPage + rowsPerPage)
          .map((e) => {
            return (
              // <p>{e.message.eventId}</p>
              <Box key="" borderWidth="1px" rounded="lg" marginBottom="1rem" padding="1rem">
                <Heading as="h4" size="md">
                  {e.message?.eventTypeId} - {e.status}
                </Heading>
                <p>
                  {formatMoment(e.message.enqueuedTimeUtc)}
                  <a href={`/Endpoints/Details/${e.message?.originatingFrom}`}>{e.message?.originatingFrom}</a>
                </p>
                <br />
                <p>
                  <b>EventId</b>
                </p>
                <p>{e.message.eventId}</p>
                <br />
                <p>
                  <b>Payload</b>
                </p>
                <pre>{JSON.stringify(JSON.parse(e.message.eventContent!), null, 2)}</pre>
                <br />
                <p>
                  <b>To</b>
                </p>
                <p>
                  <a href={`/Endpoints/Details/${e.message?.to}`}>{e.message?.to}</a>
                </p>
              </Box>
            );
          })}
      </Box>
    </Box>
  );
}
