import * as React from "react";
const { useEffect, useState } = React;

import * as api from "api-client";
import * as moment from "moment";
import { useParams } from "react-router-dom";
import { EndpointStatus, formatMoment, getEndpointStatus } from "functions/endpoint.functions";
import DataTable, { ITableRow, ITableHeadAction } from "components/data-table/data-table";
import { ITableHeadCell } from "components/data-table/data-table-header";
import Page from "components/page";
import TabSelection, { ITab } from "components/tab-selection";

import FailedTab from "components/endpoint-details/tabs/failed-tab";
import PendingTab from "components/endpoint-details/tabs/pending-tab";
import EventTypeTab from "components/endpoint-details/tabs/event-type-tab";
import SubscriptionsTab from "components/endpoint-details/tabs/subscriptions-tab";
import SearchTab from "components/endpoint-details/tabs/search-tab";
import MetadataTab from "components/endpoint-details/tabs/metadata-tab";

export interface ComposeNewResponse {
  hasError: boolean;
  responseString: string;
}

type EndpointDetailsProps = {
  endpointState: api.EndpointStatus;
};

const EndpointDetails = (props: EndpointDetailsProps) => {
  const client = new api.Client(api.CookieAuth());
  const params = useParams();

  const [endpointStatus, setEndPointEndpointStatus] = useState<api.EndpointStatus>(props.endpointState);
  const [pageWidth, setPageWidth] = useState<number>(0);
  const [pageHeight, setPageHeight] = useState<number>(0);

  const [unsupportedEvents, setUnsupportedEvents] = useState<api.Event[]>([]);
  const [dlqEvents, setDlqEvents] = useState<api.Event[]>([]);
  const [roleAssignmentScript, setRoleAssignmentScript] = useState<string>("No script available");

  // FailedTab
  const [isFailedTabEnabled, setIsFailedTabEnabled] = useState<boolean>(false);

  // PendingTab
  const [isPendingTabEnabled, setIsPendingTabEnabled] = useState<boolean>(false);

  // SubscriptionTab
  const [isSubscriptionTabEnabled, setIsSubscriptionTabEnabled] = useState<boolean>(false);

  // Useeffect to retrieve general details
  useEffect(() => {
    const fetchData = async () => {
      // Role Assignment Script
      client.getEndpointRoleAssignmentScript(params.id!).then((res) => {
        setRoleAssignmentScript(res);
      });
    };

    fetchData();
  }, []);

  // Useeffect to retrieve endpoint details (failed, unsupported, deadletter) (first paging 40 events)
  useEffect(() => {
    const fetchData = async () => {
      const tempEndpoint = props.endpointState ? api.EndpointStatus.fromJS(props.endpointState) : await client.getApiEndpointstatusStatusEndpointName(params.id!);
      setEndPointEndpointStatus(tempEndpoint);

      // Unsupported Events
      tempEndpoint.unsupportedEvents?.forEach(async (eventSessionId) => {
        const eventSessionSplit = eventSessionId.split("_");
        const eventId = eventSessionSplit[0];
        const sessionId = eventSessionSplit[1];
        const event = await client.getEventUnsupportedEndpointIdEventId(params.id!, eventId, sessionId);

        const unsupportedCopy = unsupportedEvents;
        unsupportedCopy.push(event);
        setUnsupportedEvents(unsupportedCopy);
      });

      // Deadletter Events
      tempEndpoint.deadletteredEvents?.forEach(async (eventSessionId) => {
        const eventSessionSplit = eventSessionId.split("_");
        const eventId = eventSessionSplit[0];
        const sessionId = eventSessionSplit[1];
        const event = await client.getEventDeadletterEndpointIdEventId(params.id!, eventId, sessionId);

        const dlqCopy = dlqEvents;
        dlqCopy.push(event);
        setDlqEvents(dlqCopy);
      });
    };

    fetchData();
  }, []);

  return (
    <>
      <Page
        offsetDimensionsHandler={(x, y) => {
          setPageHeight(y);
          setPageWidth(x);
        }}
        title={params.id! + " details "}
      >
        <TabSelection
          tabs={[
            {
              name: "Failed",
              content: <FailedTab key="failed" setIsTabEnabled={setIsFailedTabEnabled} endpointStatus={endpointStatus} roleAssignmentScript={roleAssignmentScript} />,
              isEnabled: isFailedTabEnabled,
            },
            {
              name: "Pending",
              content: <PendingTab key="pending" setIsTabEnabled={setIsPendingTabEnabled} roleAssignmentScript={roleAssignmentScript} />,
              isEnabled: isPendingTabEnabled,
            },
            {
              name: "Search",
              content: <SearchTab key="search" roleAssignmentScript={roleAssignmentScript} />,
              isEnabled: true,
            },
            {
              name: "Event Types",
              content: <EventTypeTab key="eventTypes" pageDimensions={{ height: pageHeight, width: pageWidth }} />,
              isEnabled: true,
            },
            {
              name: "Alerts",
              content: <SubscriptionsTab key="Alerts" setIsTabEnabled={setIsSubscriptionTabEnabled} roleAssignmentScript={roleAssignmentScript} />,
              isEnabled: isSubscriptionTabEnabled,
            },
            {
              name: "Information",
              content: <MetadataTab key="Information" />,
              isEnabled: true,
            },
          ]}
        />
      </Page>
    </>
  );
};

export default EndpointDetails;
