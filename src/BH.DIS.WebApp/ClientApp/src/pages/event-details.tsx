import * as React from "react";
import * as api from "api-client";
import Page from "components/page";
import HistoryListing from "components/event-details/history-listing";
import LogListing from "components/event-details/log-listing";
import MessageListing from "components/event-details/message-listing";
import { useParams } from "react-router-dom";
import TabSelection from "components/tab-selection";
import Loading from "components/loading/loading";
import { Flex } from "@chakra-ui/react";
import AuditListing from "components/event-details/audit-listing";
import BlockedListing from "components/event-details/blocked-listing";
const { useEffect, useState } = React;

export interface BlockedEvent {
  message: api.Message;
  status: string;
}

export interface PendingEvent {
  message: api.Message;
  status: string;
}

type EventDetailsProps = {
  eventDetails: api.EventDetails;
  backIndex: number;
};

const EventDetails = (props: EventDetailsProps) => {
  const client = new api.Client(api.CookieAuth());
  const params = useParams();

  const [cosmosEvent, setCosmosEvent] = useState<api.Event>();
  const [eventDetails, setEventDetails] = useState<api.EventDetails>(props.eventDetails);
  const [eventTypes, setEventTypes] = useState<api.EventType[]>([]);
  const [logs, setLogs] = useState<api.EventLogEntry[]>([]);
  const [histories, setHistories] = useState<api.Message[]>([]);
  const [audits, setAudits] = useState<api.MessageAudit[]>([]);
  const [blockedEvents, setBlockedEvents] = useState<BlockedEvent[]>([]);
  const [blockedEventIds, setBlockedEventIds] = useState<api.BlockedEvent[]>([]);

  useEffect(() => {
    const fetchData = async () => {
      const tempEventDetails = props.eventDetails ? api.EventDetails.fromJS(props.eventDetails) : await client.getEventDetailsId(params.id!, params.endpointId!); // messagestore

      const tempCosmosEvent = await client.getEventId(params.id!, params.endpointId!);

      // Completed events don't have eventcontent set in cosmos. For completed events we therefore use the data from messagestore.
      if (tempCosmosEvent.resolutionStatus?.toLowerCase() == "completed") {
        tempCosmosEvent.messageContent = new api.MessageContent();
        tempCosmosEvent.messageContent.eventContent = new api.EventContent();
        tempCosmosEvent.messageContent.eventContent.eventJson = tempEventDetails.failedMessage?.eventContent;
      }

      setEventDetails(tempEventDetails);
      setCosmosEvent(tempCosmosEvent);

      if (tempCosmosEvent.resolutionStatus?.toLowerCase() === "failed" || tempCosmosEvent.resolutionStatus?.toLowerCase() === "unsupported" || tempCosmosEvent.resolutionStatus?.toLowerCase() === "deadlettered")
        client
          .getEventBlockedId(tempEventDetails.failedMessage?.sessionId!, tempEventDetails.failedMessage?.endpointId!) //cosmos
          .then(async (res) => {
            const tempBlockedEvents = [];

            for (const event of res.slice(0, 6)) {
              const eventIds = await client.getEventIds(event.eventId!, event.originatingId!); // messagestore
              tempBlockedEvents.push({ message: eventIds, status: event.status! });
            }
            setBlockedEvents(tempBlockedEvents);
            setBlockedEventIds(res);
          });

      client
        .getEventDetailsLogsId(params.id!, params.endpointId!) // applicationinsight
        .then((res) => {
          setLogs(res);
        });

      client
        .getEventDetailsHistoryId(params.id!, params.endpointId!) // messagestore
        .then((res) => {
          setHistories(res);
        });

      client
        .getMessageAuditsEventId(params.id!) // messagestore
        .then((res) => {
          setAudits(res);
        });

      client.getEventTypes().then((res) => {
        setEventTypes(res);
      });
    };
    fetchData();
  }, []);

  const fetchBlockedEvents = async (startIndex: number, endIndex: number) => {
    const tempBlockedEvents = [];

    for (const event of blockedEventIds.slice(startIndex, endIndex)) {
      const res = await client.getEventIds(event.eventId!, event.originatingId!);
      if (res) {
        tempBlockedEvents.push({ message: res, status: event.status! });
        setBlockedEvents(tempBlockedEvents);
      }
    }
  };

  const skipEvent = async (eventId: string, messageId: string) => {
    await client.postSkipEventIds(eventId, messageId);
  };

  const resubmitEvent = async (eventId: string, messageId: string) => {
    await client.postResubmitEventIds(eventId, messageId);
  };

  const resubmitEventWithChanges = async (eventId: string, messageId: string, body: api.ResubmitWithChanges) => {
    await client.postResubmitWithChangesEventIds(eventId, messageId, body);
  };

  const tabs = () => {
    let blockedCount;
    if (blockedEventIds.length != null) {
      blockedCount = blockedEventIds?.length > 99 ? "99+" : blockedEventIds?.length;
    }

    return [
      {
        name: `Message`,
        isEnabled: true,
        content: <MessageListing resubmitEventWithChanges={resubmitEventWithChanges} resubmitEvent={resubmitEvent} skipEvent={skipEvent} eventTypes={eventTypes} eventDetails={cosmosEvent} key="Message" />,
      },
      {
        name: `History (${histories.length ?? 0})`,
        isEnabled: histories.length > 0,
        content: <HistoryListing histories={histories} key="History" />,
      },
      {
        name: `Logs (${logs?.length ?? 0})`,
        isEnabled: logs.length > 0,
        content: <LogListing logs={logs!} key="Logs" />,
      },
      {
        name: `Audit (${audits?.length ?? 0})`,
        isEnabled: audits.length > 0,
        content: <AuditListing audits={audits!} key="Audits" />,
      },
      {
        name: `Blocked (${blockedCount})`,
        isEnabled: blockedEventIds.length > 0,
        content: <BlockedListing totalItems={blockedEventIds.length} fetchBlockedEvents={fetchBlockedEvents} events={blockedEvents} key="Blocked" />,
      },
    ];
  };

  return (
    <>
      {eventDetails === undefined ? (
        <Flex flex="1" justifyContent="center" alignItems="center">
          <Loading />
        </Flex>
      ) : (
        <Page title="Event Details" backbutton={true} backUrl={`/Endpoints/Details/${params.endpointId!}`} backIndex={params.backindex!}>
          <TabSelection tabs={tabs()} />
        </Page>
      )}
    </>
  );
};

export default EventDetails;
