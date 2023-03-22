import * as React from "react";
import * as api from "api-client";
import { Box, Heading, Code, Textarea } from "@chakra-ui/react";
import { formatMoment } from "functions/endpoint.functions";
import { BlockedEvent } from "pages/event-details";
import TablePagination from "@material-ui/core/TablePagination";
import DataTable, { ITableHeadAction, ITableRow } from "components/data-table/data-table";
import { ITableHeadCell } from "components/data-table/data-table-header";

export interface IBlockedListing {
    events: BlockedEvent[];
    onPageChange?: () => void;
    fetchBlockedEvents:(startIndex:number, endIndex:number) => void;
    totalItems: number;
}


export default function BlockedListing(props: IBlockedListing) {
  const count = props.totalItems ?? 0;
  const [page, setPage] = React.useState(0);
  const [rowsPerPage, setRowsPerPage] = React.useState(6); // Events pr. page
  const [rows, setRows] = React.useState<ITableRow[]>([]);

  React.useEffect(() => {
    setRows(mapEvents());
  },[props.events]);

  const headCells: ITableHeadCell[] = [
    { id: "eventId", label: "Event Id", numeric: false },
    { id: "sessionId", label: "Session Id", numeric: false },
    { id: "eventTypeId", label: "Event Type", numeric: false },
    { id: "added", label: "Added(UTC)", numeric: false },
  ];

  const headActions: ITableHeadAction[] = [
  ];

  const mapEvents = () => {
    const iRows = props.events.map(item => {
        const row: ITableRow = {
            id: item.message.eventId!,
            hoverText: item.message.eventContent,
            bodyActions: [
            ],
            data: new Map([
                [
                    "eventId", 
                    { 
                        value: item.message.eventId!, 
                        searchValue: 
                        item.message.eventId! 
                    }
                ],
                [
                    "sessionId",
                    {
                        value: item.message.sessionId!,
                        searchValue: item.message.sessionId!
                    }
                ],
                [
                    "eventTypeId",
                    {
                        value: item?.message.eventTypeId,
                        searchValue: item?.message.eventTypeId || "",
                    },
                ],
                [
                    "added",
                    {
                        value: formatMoment(item.message.enqueuedTimeUtc!, true),
                        searchValue: formatMoment(item.message.enqueuedTimeUtc!, true)
                    }],
                
            ])
        }
        return row;
    });
    return iRows;
}

  return (
    <Box width="100%" marginRight="1rem" display="flex" flexDirection="column">
        <DataTable
            headCells={headCells}
            headActions={headActions}
            rows={rows}
            withCheckboxes={false}
            noDataMessage="No events available"
            isLoading={false}
            count={rows.length}
            hideDense={true}
            dataRowsPerPage={20}
        />
    </Box>
    
  );
}
