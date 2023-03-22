import * as React from "react";
import * as api from "api-client";
import DataTable, { ITableHeadAction, ITableRow } from "components/data-table/data-table";
import { useParams } from "react-router-dom";
import { formatMoment } from "functions/endpoint.functions";
import { ITableHeadCell } from "components/data-table/data-table-header";
import { Dispatch, SetStateAction } from "react";

interface ICompletedTabProps {
  setIsTabEnabled: React.Dispatch<React.SetStateAction<boolean>>;
  roleAssignmentScript: string;
}

const CompletedTab = (props: ICompletedTabProps) => {
  const client = new api.Client(api.CookieAuth());
  const params = useParams();
  const [completedEvents, setCompletedEvents] = React.useState<api.Event[]>([]);
  const [rows, setRows] = React.useState<ITableRow[]>([]);
  const [continuationToken, setContinuationToken] = React.useState<string>();

  React.useEffect(() => {
    props.setIsTabEnabled(false);
    const fetchData = async () => {
      const filter: api.EventFilter = new api.EventFilter();
      filter.endpointId = params.id!;
      filter.resolutionStatus = [api.ResolutionStatus.Completed];

      const reqBody: api.SearchRequest = new api.SearchRequest();
      reqBody.eventFilter = filter;

      const tempCompletedEventsResponse = await client.postApiEventEndpointIdGetByFilter(params.id!, reqBody);
      setContinuationToken(tempCompletedEventsResponse.continuationToken);
      const tempCompletedEvents = tempCompletedEventsResponse.events!;
      setCompletedEvents(tempCompletedEvents);

      if (tempCompletedEvents.length > 0) props.setIsTabEnabled(true);
      else props.setIsTabEnabled(false);
    };

    fetchData();
  }, []);

  React.useEffect(() => {
    setRows(mapCompletedEvents());
  }, [completedEvents]);

  const nextPage = async () => {
    if (continuationToken !== "" && continuationToken !== null && continuationToken !== "null") {
      const filter: api.EventFilter = new api.EventFilter();
      filter.endpointId = params.id!;
      filter.resolutionStatus = [api.ResolutionStatus.Completed];

      const reqBody: api.SearchRequest = new api.SearchRequest();
      reqBody.eventFilter = filter;
      reqBody.continuationToken = continuationToken;

      const tempCompletedEventsResponse = await client.postApiEventEndpointIdGetByFilter(params.id!, reqBody);

      setContinuationToken(tempCompletedEventsResponse.continuationToken);
      const tempCompletedEvents = tempCompletedEventsResponse.events!;

      //Append unique data
      setCompletedEvents(completedEvents.concat(tempCompletedEvents));
    }
  };

  const mapCompletedEvents = () => {
    const iRows = completedEvents.map((item) => {
      const row: ITableRow = {
        route: `/Message/Index/${params.id!}/${item.eventId}`,
        hoverText: item.messageContent?.eventContent?.eventJson,
        id: item.eventId!,
        bodyActions: [],
        data: new Map([
          [
            "eventId",
            {
              value: item.eventId!,
              searchValue: item.eventId!,
            },
          ],
          [
            "sessionId",
            {
              value: item.sessionId!,
              searchValue: item.sessionId!,
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
              value: formatMoment(item.updatedAt!, true),
              searchValue: formatMoment(item.updatedAt!, true),
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
    { id: "sessionId", label: "Session Id", numeric: false },
    { id: "eventTypeId", label: "Event Type", numeric: false },
    { id: "updated", label: "Updated(UTC)", numeric: false },
    { id: "added", label: "Added(UTC)", numeric: false },
  ];

  const headActions: ITableHeadAction[] = [];

  return (
    <DataTable
      headCells={headCells}
      headActions={headActions}
      rows={rows}
      withCheckboxes={false}
      noDataMessage="No events available"
      isLoading={false}
      count={rows.length}
      subscribe={params.id!}
      hideDense={true}
      roleAssignmentScript={props.roleAssignmentScript}
      dataRowsPerPage={20}
      onPageChange={nextPage}
    />
  );
};

export default CompletedTab;
