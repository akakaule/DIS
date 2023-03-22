import * as React from "react";
import * as api from "api-client";
import DataTable, { ITableHeadAction, ITableRow, ITableBodyAction } from "components/data-table/data-table";
import { useParams } from "react-router-dom";
import { formatMoment } from "functions/endpoint.functions";
import { ITableHeadCell } from "components/data-table/data-table-header";
import { Dispatch, SetStateAction } from "react";

interface IFailedTabProps {
  endpointStatus: api.EndpointStatus;
  setIsTabEnabled: React.Dispatch<React.SetStateAction<boolean>>;
  roleAssignmentScript: string;
}

interface SessionState {
  deferredCount: number;
  pendingCount: number;
  failedCount: number;
}

const FailedTab = (props: IFailedTabProps) => {
  const client = new api.Client(api.CookieAuth());
  const params = useParams();
  const [failedEvents, setFailedEvents] = React.useState<api.Event[]>([]);
  const [sessions, setSessions] = React.useState<any>(undefined);
  const [rows, setRows] = React.useState<ITableRow[]>([]);
  const [continuationToken, setContinuationToken] = React.useState<any>("");
  const [unsupportedContinuationToken, setUnsupportedContinuationToken] = React.useState<any>("");
  const [DLQContinuationToken, setDLGContinuationToken] = React.useState<any>("");

  React.useEffect(() => {
    const fetchData = async () => {
      if (!props.endpointStatus?.failedEvents) return;

      const filter: api.EventFilter = new api.EventFilter();
      filter.endpointId = params.id!;
      filter.resolutionStatus = [api.ResolutionStatus.Failed];

      //Failed
      const reqBody: api.SearchRequest = new api.SearchRequest();
      reqBody.eventFilter = filter;
      const tempFailedEventsResponse = await client.postApiEventEndpointIdGetByFilter(params.id!, reqBody);
      const tempFailedEvents = tempFailedEventsResponse.events!;
      setContinuationToken(tempFailedEventsResponse.continuationToken);
      const sessionStatus: api.SessionStatus[] = await Promise.all(
        tempFailedEvents.map(async (event) => {
          return client.getEndpointSessionId(params.id!, event.sessionId!);
        })
      );

      const defferedEvents = sessionStatus.map((status) => status.deferredEvents!).reduce((pre, cur) => pre.concat(cur), []);

      const pendingEvents = sessionStatus.map((status) => status.pendingEvents!).reduce((pre, cur) => pre.concat(cur), []);

      const tempSessions = getSessions(defferedEvents, pendingEvents);
      setSessions(tempSessions);

      //Unsupported
      filter.resolutionStatus = [api.ResolutionStatus.Unsupported];
      reqBody.eventFilter = filter;
      const tempUnsupportedEventsResponse = await client.postApiEventEndpointIdGetByFilter(params.id!, reqBody);
      const tempUnsupportedEvents = tempUnsupportedEventsResponse.events!;
      setUnsupportedContinuationToken(tempUnsupportedEventsResponse.continuationToken);

      //Deadlettered
      filter.resolutionStatus = [api.ResolutionStatus.DeadLettered];
      reqBody.eventFilter = filter;
      const tempDLQEventsResponse = await client.postApiEventEndpointIdGetByFilter(params.id!, reqBody);
      const tempDLQEvents = tempDLQEventsResponse.events!;
      setDLGContinuationToken(tempDLQEventsResponse.continuationToken);

      setFailedEvents([...tempFailedEvents, ...tempUnsupportedEvents, ...tempDLQEvents]);

      if (tempFailedEvents.length > 0 || tempUnsupportedEvents.length > 0 || tempDLQEvents.length > 0) props.setIsTabEnabled(true);
      else props.setIsTabEnabled(false);
    };

    fetchData();
  }, [props, props.endpointStatus]);

  React.useEffect(() => {
    setRows(mapFailedEvents());
  }, [failedEvents]);

  const getSessions = (defferedEvents: string[], pendingEvents: string[]) => {
    const tempSessions: { [key: string]: SessionState } = {};

    defferedEvents.forEach((event) => {
      const eventSessionSplit = event.split("_");
      const sessionId = eventSessionSplit[1];
      if (tempSessions[sessionId] !== undefined) tempSessions[sessionId].deferredCount = tempSessions[sessionId].deferredCount + 1;
      else
        tempSessions[sessionId] = {
          deferredCount: 1,
          pendingCount: 0,
          failedCount: 0,
        };
    });

    pendingEvents.forEach((event) => {
      const eventSessionSplit = event.split("_");
      const sessionId = eventSessionSplit[1];
      if (tempSessions[sessionId] !== undefined) tempSessions[sessionId].pendingCount = tempSessions[sessionId].pendingCount + 1;
      else
        tempSessions[sessionId] = {
          deferredCount: 0,
          pendingCount: 1,
          failedCount: 0,
        };
    });
    return tempSessions;
  };

  const removeFromTable = (event: api.Event) => {
    const index = failedEvents?.indexOf(event);
    if (index !== undefined && index > -1) {
      failedEvents?.splice(index, 1);
      setFailedEvents([...failedEvents]);
    }
  };

  const skipSingleFailedEvent = (event: api.Event) => {
    removeFromTable(event);
    client.postSkipEventIds(event.eventId!, event.lastMessageId!);
  };

  const resubmitSingleEvent = (event: api.Event) => {
    removeFromTable(event);
    client.postResubmitEventIds(event.eventId!, event.lastMessageId!);
  };

  const getViableBodyActions = (event: api.Event): ITableBodyAction[] => {
    switch (event.resolutionStatus?.toLowerCase()) {
      case "failed":
      case "deadlettered":
      case "unsupported":
        return [
          {
            name: "Resubmit",
            onClick: () => {
              resubmitSingleEvent(event);
              return false;
            },
          },
          {
            name: "Skip",
            onClick: () => {
              skipSingleFailedEvent(event);
              return false;
            },
          },
        ];
      default:
        return [];
    }
  };

  const mapFailedEvents = (): ITableRow[] => {
    const iRows = failedEvents.map((item) => {
      const row: ITableRow = {
        id: item.eventId!,
        hoverText: item.messageContent?.eventContent?.eventJson,
        route: `/Message/Index/${params.id!}/${item.eventId}/0`,
        bodyActions: getViableBodyActions(item),
        data: new Map([
          ["eventID", { value: item.eventId!, searchValue: item.eventId! }],
          [
            "pendingCount",
            {
              value: sessions[item.sessionId!]?.pendingCount ?? 0,
              searchValue: sessions[item.sessionId!]?.pendingCount ?? 0,
            },
          ],
          [
            "deferredCount",
            {
              value: sessions[item.sessionId!]?.deferredCount ?? 0,
              searchValue: sessions[item.sessionId!]?.deferredCount ?? 0,
            },
          ],
          [
            "status",
            {
              value: item.resolutionStatus,
              searchValue: item.resolutionStatus,
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

  const doActionSelectedRows = (iRows: ITableRow[], actionName: string) => {
    iRows.forEach((row) => {
      const action = row.bodyActions?.find((a) => a.name === actionName);
      action?.onClick.call("");
    });
  };

  const nextPage = async () => {
    const filter: api.EventFilter = new api.EventFilter();
    filter.endpointId = params.id!;
    const reqBody: api.SearchRequest = new api.SearchRequest();

    let tempUnsupportedEvents: api.Event[] = [];
    let tempDLQEvents: api.Event[] = [];
    let tempFailedEvents: api.Event[] = [];

    //Failed
    if (continuationToken !== "" && continuationToken !== null && continuationToken !== "null") {
      reqBody.continuationToken = continuationToken;
      filter.resolutionStatus = [api.ResolutionStatus.Failed];
      reqBody.eventFilter = filter;
      const tempFailedEventsResponse = await client.postApiEventEndpointIdGetByFilter(params.id!, reqBody);
      tempFailedEvents = tempFailedEventsResponse.events!;
      setContinuationToken(tempFailedEventsResponse.continuationToken);

      const sessionStatus: api.SessionStatus[] = await Promise.all(
        tempFailedEvents.map(async (event) => {
          return client.getEndpointSessionId(params.id!, event.sessionId!);
        })
      );

      const defferedEvents = sessionStatus.map((status) => status.deferredEvents!).reduce((pre, cur) => pre.concat(cur), []);

      const pendingEvents = sessionStatus.map((status) => status.pendingEvents!).reduce((pre, cur) => pre.concat(cur), []);

      const tempSessions = getSessions(defferedEvents, pendingEvents);

      setSessions({ ...sessions, ...tempSessions });
    }

    //Unsupported
    if (unsupportedContinuationToken !== "" && unsupportedContinuationToken !== null && unsupportedContinuationToken !== "null") {
      reqBody.continuationToken = unsupportedContinuationToken;
      filter.resolutionStatus = [api.ResolutionStatus.Unsupported];
      const tempUnsupportedEventsResponse = await client.postApiEventEndpointIdGetByFilter(params.id!, reqBody);
      tempUnsupportedEvents = tempUnsupportedEventsResponse.events!;
      setUnsupportedContinuationToken(tempUnsupportedEventsResponse.continuationToken);
    }

    //DLQ
    if (DLQContinuationToken !== "" && DLQContinuationToken !== null && DLQContinuationToken !== "null") {
      reqBody.continuationToken = DLQContinuationToken;
      filter.resolutionStatus = [api.ResolutionStatus.DeadLettered];
      const tempDLQEventsResponse = await client.postApiEventEndpointIdGetByFilter(params.id!, reqBody);
      tempDLQEvents = tempDLQEventsResponse.events!;
      setDLGContinuationToken(tempDLQEventsResponse.continuationToken);
    }

    setFailedEvents([...failedEvents, ...tempFailedEvents, ...tempUnsupportedEvents, ...tempDLQEvents]);
  };

  const headCells: ITableHeadCell[] = [
    { id: "eventId", label: "Event Id", numeric: false },
    { id: "pendingCount", label: "Pending", numeric: true },
    { id: "deferredCount", label: "Deferred", numeric: true },
    { id: "status", label: "Status", numeric: false },
    { id: "sessionId", label: "Session Id", numeric: false },
    { id: "eventTypeId", label: "Event Type", numeric: false },
    { id: "updated", label: "Updated(UTC)", numeric: false },
    { id: "added", label: "Added(UTC)", numeric: false },
  ];

  const headActions: ITableHeadAction[] = [
    {
      name: "Resubmit",
      onClick: (selectedRows: ITableRow[]) => {
        doActionSelectedRows(selectedRows, "Resubmit");
        //onPageChange();
        return false;
      },
    },
    {
      name: "Skip",
      onClick: (selectedRows: ITableRow[]) => {
        doActionSelectedRows(selectedRows, "Skip");
        //onPageChange();
        return false;
      },
    },
  ];

  return (
    <DataTable
      headCells={headCells}
      headActions={headActions}
      rows={rows}
      withCheckboxes={true}
      noDataMessage="No events available"
      isLoading={false}
      count={rows.length ?? 0}
      subscribe={params.id!}
      hideDense={true}
      dataRowsPerPage={20}
      onPageChange={nextPage}
      fixedWidth={"-webkit-fill-available"}
    />
  );
};

export default FailedTab;
