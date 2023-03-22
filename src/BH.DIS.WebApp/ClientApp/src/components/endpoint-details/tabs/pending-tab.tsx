import * as React from "react";
import * as api from "api-client";
import DataTable, { ITableHeadAction, ITableRow } from "components/data-table/data-table";
import { useParams } from "react-router-dom";
import { formatMoment } from "functions/endpoint.functions";
import { ITableHeadCell } from "components/data-table/data-table-header";
import { Dispatch, SetStateAction } from "react";

interface IPendingTabProps {
  setIsTabEnabled: React.Dispatch<React.SetStateAction<boolean>>;
  roleAssignmentScript: string;
}

const PendingTab = (props: IPendingTabProps) => {
  const client = new api.Client(api.CookieAuth());
  const params = useParams();
  const [pendingEvents, setPendingEvents] = React.useState<api.Event[]>([]);
  const [rows, setRows] = React.useState<ITableRow[]>([]);
  const [continuationToken, setContinuationToken] = React.useState<string>();

  React.useEffect(() => {
    props.setIsTabEnabled(false);
    const fetchData = async () => {
      const filter: api.EventFilter = new api.EventFilter();
      filter.endpointId = params.id!;
      filter.resolutionStatus = [api.ResolutionStatus.Pending];

      const reqBody: api.SearchRequest = new api.SearchRequest();
      reqBody.eventFilter = filter;

      const tempPendingEventsReponse = await client.postApiEventEndpointIdGetByFilter(params.id!, reqBody);
      const tempPendingEvents = tempPendingEventsReponse.events!;
      setPendingEvents(tempPendingEvents);

      setContinuationToken(tempPendingEventsReponse.continuationToken);

      if (tempPendingEvents.length > 0) props.setIsTabEnabled(true);
      else props.setIsTabEnabled(false);
    };

    fetchData();
  }, []);

  React.useEffect(() => {
    setRows(mapPendingEvents());
  }, [pendingEvents]);

  const nextPage = async () => {
    if (continuationToken !== "" && continuationToken !== null && continuationToken !== "null") {
      const filter: api.EventFilter = new api.EventFilter();
      filter.endpointId = params.id!;
      filter.resolutionStatus = [api.ResolutionStatus.Pending];

      const reqBody: api.SearchRequest = new api.SearchRequest();
      reqBody.eventFilter = filter;
      reqBody.continuationToken = continuationToken;

      const tempPendingEventsReponse = await client.postApiEventEndpointIdGetByFilter(params.id!, reqBody);

      setContinuationToken(tempPendingEventsReponse.continuationToken);
      const tempPendingEvents = tempPendingEventsReponse.events!;

      //Append data
      setPendingEvents(pendingEvents.concat(tempPendingEvents));
    }
  };

  const mapPendingEvents = () => {
    const iRows = pendingEvents.map((item) => {
      const row: ITableRow = {
        id: item.eventId!,
        hoverText: item.messageContent?.eventContent?.eventJson,
        route: `/Message/Index/${params.id!}/${item.eventId}/1`,
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
      fixedWidth={"-webkit-fill-available"}
    />
  );
};

export default PendingTab;
