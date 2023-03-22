import * as React from "react";
import * as api from "api-client";
import DataTable, { ITableHeadAction, ITableRow } from "components/data-table/data-table";
import { useParams } from "react-router-dom";
import { formatMoment } from "functions/endpoint.functions";
import { ITableHeadCell } from "components/data-table/data-table-header";
import EventFiltering from "../filter/event-filtering";
import FilterContext, { IFilterContext } from "../filter/filtering-context";
import { Stack } from "@chakra-ui/react";

interface ISearchTabProps {
  roleAssignmentScript: string;
}

const SearchTab = (props: ISearchTabProps) => {
  const client = new api.Client(api.CookieAuth());
  const params = useParams();
  const [events, setEvents] = React.useState<api.Event[]>([]);
  const [rows, setRows] = React.useState<ITableRow[]>([]);
  const [continuationToken, setContinuationToken] = React.useState<string>();

  const [eventFilter, setEventFilter] = React.useState<api.EventFilter>(new api.EventFilter());
  const context: IFilterContext = {
    filterContext: eventFilter,
    setProjectContext: setEventFilter,
  };
  const filterContext = React.useContext(FilterContext);

  React.useEffect(() => {
    setRows(mapEvents());
  }, [events]);

  React.useEffect(() => {
    const tempEventFilter = new api.EventFilter();
    tempEventFilter.endpointId = params.id!;
    setEventFilter(tempEventFilter);
  }, []);

  const mapEvents = (): ITableRow[] => {
    const iRows = events.map((item) => {
      const row: ITableRow = {
        id: item.eventId!,
        hoverText: item.messageContent?.eventContent?.eventJson,
        route: `/Message/Index/${params.id!}/${item.eventId}/2`,
        bodyActions: [],
        data: new Map([
          ["eventID", { value: item.eventId!, searchValue: item.eventId! }],
          [
            "status",
            {
              value: item.resolutionStatus!,
              searchValue: item.resolutionStatus!,
            },
          ],
          [
            "sessionId",
            {
              value: item?.sessionId,
              searchValue: item?.sessionId || "",
            },
          ],
          [
            "eventTypeId",
            {
              value: item?.eventTypeId,
              searchValue: item?.eventTypeId || "",
            },
          ],
          [
            "updated",
            {
              value: formatMoment(item?.updatedAt, true),
              searchValue: formatMoment(item?.updatedAt) || "",
            },
          ],
          [
            "added",
            {
              value: formatMoment(item?.enqueuedTimeUtc, true),
              searchValue: formatMoment(item?.enqueuedTimeUtc) || "",
            },
          ],
        ]),
      };
      return row;
    });
    return iRows;
  };

  const headCells: ITableHeadCell[] = [
    { id: "eventId", label: "Event Id", numeric: false },
    { id: "status", label: "Status", numeric: false },
    { id: "sessionId", label: "Session Id", numeric: false },
    { id: "eventTypeId", label: "Event Type", numeric: false },
    { id: "updated", label: "Updated(UTC)", numeric: false },
    { id: "added", label: "Added(UTC)", numeric: false },
  ];

  const nextPage = async () => {
    if (continuationToken !== "" && continuationToken !== null && continuationToken !== "null") {
      const reqBody: api.SearchRequest = new api.SearchRequest();
      reqBody.eventFilter = eventFilter;
      reqBody.continuationToken = continuationToken;

      const tempSearchEventResponse = await client.postApiEventEndpointIdGetByFilter(params.id!, reqBody);

      setContinuationToken(tempSearchEventResponse.continuationToken);
      const tempSearchEvents = tempSearchEventResponse.events!;

      //Append data
      setEvents(events.concat(tempSearchEvents));
    }
  };

  const headActions: ITableHeadAction[] = [];
  const handleFilterClicked = async (filter: api.EventFilter) => {
    const reqBody: api.SearchRequest = new api.SearchRequest();
    reqBody.eventFilter = filter;
    client.postApiEventEndpointIdGetByFilter(params.id!, reqBody).then((res) => {
      setEvents(res?.events!);
      setContinuationToken(res.continuationToken);
    });
  };

  return (
    <>
      <FilterContext.Provider value={context}>
        <Stack>
          <EventFiltering handleFilterClicked={handleFilterClicked} />
          <DataTable
            headCells={headCells}
            headActions={headActions}
            rows={rows}
            noDataMessage="No events available"
            isLoading={false}
            count={rows.length ?? 0}
            subscribe={params.id!}
            hideDense={true}
            roleAssignmentScript={props.roleAssignmentScript}
            dataRowsPerPage={40}
            onPageChange={nextPage}
          />
        </Stack>
      </FilterContext.Provider>
    </>
  );
};

export default SearchTab;
