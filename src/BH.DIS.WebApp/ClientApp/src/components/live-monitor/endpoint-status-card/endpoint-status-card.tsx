import React, { ReactNode } from "react";
import { Box, Heading, Badge, Link } from "@chakra-ui/react";
import * as api from "api-client";
import shadowStyles from "shared-styles/shadows";
import { EndpointStatus, getEndpointStatus, mapHeartbeatStatusToColor, mapStatusToColor, HeartbeatStatus } from "functions/endpoint.functions";

type EndpointStatusCardProps = {
  statusCount: api.IEndpointStatusCount;
  heartbeatStatus: HeartbeatStatus;
};

const EndpointStatusCard = (props: EndpointStatusCardProps) => {
  const status: EndpointStatus = getEndpointStatus(props.statusCount);

  const header = (
    <Box p="4" borderWidth="0px" overflow="hidden" bg={`${mapStatusToColor(status)}.500`}>
      <Heading size="md" noOfLines={1}>
        {props.statusCount.endpointId !== undefined ? props.statusCount.endpointId.charAt(0)?.toUpperCase() + props.statusCount.endpointId.slice(1) : ""}
      </Heading>
    </Box>
  );

  const body = (
    <Box p="4" bg="gray.100">
      <Box dir="flex" alignItems="baseline">
        <Box color="gray.700" fontWeight="semibold" letterSpacing="wide" fontSize="xs" textTransform="uppercase" ml="2">
          {getCombinedEventStates(props.statusCount)}
        </Box>
      </Box>

      <Badge rounded="full" px="2" colorScheme={mapStatusToColor(status)}>
        {status.toUpperCase()}
      </Badge>

      <Badge rounded="full" px="2" marginLeft={"6px"} colorScheme={mapHeartbeatStatusToColor(props.heartbeatStatus)}>
        {props.heartbeatStatus.toUpperCase()}
      </Badge>

      <Box display="flex" justifyContent="flex-end" color="gray.700" as="h6" fontSize="sm" fontWeight="100" paddingTop=".5em">
        {props.statusCount.eventTime && <span>Last updated {props.statusCount.eventTime?.fromNow()}</span>}
      </Box>
    </Box>
  );

  return (
    <Link href={`/Endpoints/Details/${props.statusCount.endpointId}`} css={[shadowStyles.shadow2, shadowStyles.hoverShadow4]} rounded="lg" overflow="hidden">
      {header}
      {body}
    </Link>
  );
};

const getCombinedEventStates = (props: api.IEndpointStatusCount): ReactNode => {
  const pending = props.pendingCount!;
  const deferred = props.deferredCount!;
  const failed = props.failedCount!;
  return (
    <React.Fragment>
      {pending} pending &bull; {deferred} deferred &bull; {failed} failed
    </React.Fragment>
  );
};

export default EndpointStatusCard;
