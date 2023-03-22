import * as React from "react";
import * as moment from "moment";
import { Box, Flex, Tooltip } from "@chakra-ui/react";
import * as api from "api-client";
import { EndpointStatus, getEndpointStatus, HeartbeatStatus, mapHeartbeatStatusToColor, mapStatusToColor } from "functions/endpoint.functions";
import DataTable, { ITableRow } from "components/data-table/data-table";
import { ITableHeadCell } from "components/data-table/data-table-header";
import Page from "components/page";
import { getApplicationStatus } from "hooks/app-status";
import EndpointPurgeButton from "components/endpoint-list/endpoint-purge-button";
import EndpointDisableButton from "components/endpoint-list/endpoint-disable-button";
import EndpointAlertsButton from "components/endpoint-list/endpoint-alerts-button";
import Icon from "@material-ui/core/Icon";
import Cookies from "js-cookie";
import EndpointDisableHeartbeatButton from "components/endpoint-list/endpoint-disable-heartbeat-button";

type EndpointProps = {
  endpointIds: string[];
  endpointStates: api.EndpointStatus[];
};
type EndpointState = {
  endpointIds: string[];
  endpointStates: api.EndpointStatusCount[];
  metadatas: api.MetadataShort[];
  loading: boolean;
  checkedEndpoints: string[];
};

enum ITableData {
  name = "name",
  failed = "failed",
  deferred = "deferred",
  pending = "pending",
  lastUpdated = "lastUpdated",
  status = "status",
  heartbeat = "heartbeat",
  subscriptionstatus = "subscription",
  actions = "actions",
}

export default class EndpointsList extends React.Component<EndpointProps, EndpointState> {
  private client: api.Client;
  private env: string | undefined;
  private cookieName = "endpointFilters";

  constructor(props: EndpointProps) {
    super(props);

    const mappedProps = this.props.endpointStates?.map((x) => this.mapMomentInEndpointStatus(x));

    this.state = {
      endpointIds: this.props.endpointIds ?? [],
      endpointStates: mappedProps ?? [],
      metadatas: [],
      loading: !mappedProps,
      checkedEndpoints: Cookies.get(this.cookieName) ? Cookies.get(this.cookieName)!.split(",") : [],
    };

    this.handleCheck = this.handleCheck.bind(this);
  }

  // componentDidMount gets called once we're client-side
  async componentDidMount() {
    const cookie = Cookies.get(this.cookieName)?.split(",");

    this.client = new api.Client(api.CookieAuth());
    // If we didn't rehydrate with endpoint states, call the api to get them
    const endPointIds = this.props.endpointIds ? this.props.endpointIds : await this.client.getEndpointsAll();
    let filteredEndpointIds: Array<string>;
    this.env = (await getApplicationStatus()).env;

    if (cookie) {
      filteredEndpointIds = cookie;
    } else {
      filteredEndpointIds = endPointIds;
    }

    const [endpoints, metadatas] = await Promise.all([this.client.postApiEndpointStatusCount(filteredEndpointIds), this.client.postApiMetadatashort(filteredEndpointIds)]);

    for (const endpoint of endpoints) {
      this.state.endpointStates.push(this.mapMomentInEndpointStatus(endpoint));
    }

    for (const metadata of metadatas) {
      this.state.metadatas.push(metadata);
    }

    for (const endpointId of endPointIds) {
      this.state.endpointIds.push(endpointId);
    }

    this.setState({ ...this.state, loading: false });
  }

  async handleCheck(endpointId: string, newState: boolean) {
    let getAll: boolean = true;
    if (newState) {
      this.state.checkedEndpoints.push(endpointId);
      this.setState({ ...this.state });
    } else {
      let nCE = this.state.checkedEndpoints.filter((e) => e !== endpointId);
      let nCEs = this.state.endpointStates.filter((e) => e.endpointId !== endpointId);
      getAll = nCE.length !== 0;
      if (getAll) {
        this.setState({ ...this.state, endpointStates: nCEs, checkedEndpoints: nCE });
      } else {
        this.state.endpointStates.pop();
        this.state.checkedEndpoints.pop();
      }
    }

    if (getAll) {
      let loadedEndpointStates: string[] = [];
      for (const item of this.state.endpointStates) {
        loadedEndpointStates.push(item.endpointId!);
      }

      let endpointStatesToGet = this.state.checkedEndpoints.filter((item) => loadedEndpointStates.indexOf(item) < 0);
      let endpoints = await this.client.postApiEndpointStatusCount(endpointStatesToGet);

      for (const endpoint of endpoints) {
        this.state.endpointStates.push(this.mapMomentInEndpointStatus(endpoint));
      }

      let newStates: api.EndpointStatusCount[] = [];
      for (const endpointId of this.state.checkedEndpoints) {
        const toPop = this.state.endpointStates.find((es) => es.endpointId === endpointId);
        if (toPop) {
          newStates.push(toPop);
        }
      }

      this.setState({ ...this.state, endpointStates: newStates });
    } else {
      this.setState({ ...this.state, loading: true });
      const endpoints = await this.client.postApiEndpointStatusCount(this.state.endpointIds);

      for (const endpoint of endpoints) {
        this.state.endpointStates.push(this.mapMomentInEndpointStatus(endpoint));
      }
      this.setState({ ...this.state, loading: false });
    }
  }

  stopLoading = (): void => {
    this.setState({ loading: false });
  };

  startLoading = (): void => {
    this.setState({ loading: true });
  };

  refreshEndpoint = async (endpointId: string): Promise<any> => {
    this.client = new api.Client(api.CookieAuth());

    const [endpoint, endpointMetadata] = await Promise.all([this.client.getEndpointStatusCountId(endpointId), this.client.getMetadataEndpoint(endpointId)]);

    const mappedEndpoint = this.mapMomentInEndpointStatus(endpoint);
    const existingEndpoints = [...this.state.endpointStates];
    const existingMetadata = [...this.state.metadatas];

    const endpointIndex = existingEndpoints.findIndex((e) => e.endpointId === endpointId);
    const metadataIndex = existingMetadata.findIndex((e) => e.endpointId === endpointId);

    existingEndpoints[endpointIndex] = mappedEndpoint;
    existingMetadata[metadataIndex] = endpointMetadata;

    this.setState({ endpointStates: [...existingEndpoints], metadatas: [...existingMetadata], loading: false });
  };

  render() {
    return <Page title="Endpoints">{this.createTableNew()}</Page>;
  }

  transformStates(endpoints: api.EndpointStatus[]): api.EndpointStatus[] {
    const transformedStates = [...(endpoints ?? [])];
    transformedStates.forEach((x) => (x.eventTime = x.eventTime ?? moment()));
    return transformedStates;
  }

  mapMomentInEndpointStatus(endpointStatus: api.EndpointStatusCount) {
    return api.EndpointStatusCount.fromJS(endpointStatus);
  }

  createTableNew(): JSX.Element {
    const tableData = this.state.endpointStates?.map((x) => this.mapEndpointStatusToTableRow(x));

    const headCells: ITableHeadCell[] = [
      { id: ITableData.name, label: "Name", numeric: false },
      { id: ITableData.failed, label: "Failed", numeric: true },
      { id: ITableData.deferred, label: "Deferred", numeric: true },
      { id: ITableData.pending, label: "Pending", numeric: true },
      { id: ITableData.lastUpdated, label: "Last updated", numeric: false },
      { id: ITableData.status, label: "Status", numeric: false },
      { id: ITableData.heartbeat, label: "Heartbeat", numeric: false },
      { id: ITableData.actions, label: "Actions", numeric: false },
    ];

    return (
      <DataTable
        headCells={headCells}
        rows={tableData || []}
        noDataMessage="No endpoints available"
        isLoading={this.state.loading}
        orderBy={ITableData.name}
        styles={{ marginLeft: "-1rem", marginRight: "-1rem" }}
        roleAssignmentScript={"No script available"}
        dataRowsPerPage={20}
        count={tableData.length}
        endpointIds={this.state.endpointIds || []}
        checkedEndpointIds={this.state.checkedEndpoints}
        checked={this.handleCheck}
      />
    );
  }

  mapEndpointStatusToTableRow(endpointStatus: api.EndpointStatusCount): ITableRow {
    const endpointId = endpointStatus.endpointId || "";
    const failedEventsCount = endpointStatus.failedCount || 0;
    const deferredEventsCount = endpointStatus.deferredCount || 0;
    const pendingEventsCount = endpointStatus.pendingCount || 0;
    const lastUpdated = endpointStatus.eventTime?.fromNow() || "";
    const endpointStatusValue = getEndpointStatus(endpointStatus);
    const metadata = this.state.metadatas[this.state.metadatas.findIndex((e) => e.endpointId === endpointId)] || undefined;
    const heartbeatStatus = HeartbeatStatus[metadata?.heartbeatStatus as keyof typeof HeartbeatStatus] || HeartbeatStatus.Unknown;

    return {
      id: endpointStatus.endpointId ?? "",
      route: `/Endpoints/Details/${endpointStatus.endpointId}`,
      data: new Map([
        [
          ITableData.name,
          {
            value: (
              <Box as="span" color={"blue.600"} fontWeight="bold">
                {endpointId}
              </Box>
            ),
            searchValue: endpointId,
          },
        ],
        [
          ITableData.failed,
          {
            value: failedEventsCount,
            searchValue: failedEventsCount,
          },
        ],
        [
          ITableData.deferred,
          {
            value: deferredEventsCount,
            searchValue: deferredEventsCount,
          },
        ],
        [
          ITableData.pending,
          {
            value: pendingEventsCount,
            searchValue: pendingEventsCount,
          },
        ],
        [
          ITableData.lastUpdated,
          {
            value: lastUpdated,
            searchValue: lastUpdated,
          },
        ],
        [
          ITableData.status,
          {
            value: this.mapStatusToIcon(endpointStatusValue),
            searchValue: endpointStatusValue.toString(),
          },
        ],
        [
          ITableData.heartbeat,
          {
            value: this.mapHeartbeatStatusToIcon(heartbeatStatus),
            searchValue: heartbeatStatus.toString(),
          },
        ],
        [
          ITableData.actions,
          {
            value: (
              <>
                {this.buildSubscribeButton(endpointId)}
                &nbsp;
                {this.buildPurgeButton(endpointId)}
                &nbsp;
                {this.buildDisableButton(endpointId, metadata?.subscriptionStatus === "active" ? "disable" : "enable")}
                &nbsp;
                {this.buildDisableHeartbeatButton(endpointId, metadata?.isHeartbeatEnabled)}
              </>
            ),
            searchValue: "Actions",
          },
        ],
      ]),
    };
  }

  buildPurgeButton = (endpointId: string): JSX.Element => {
    const isEnabled = !(this.env === "prod" || this.env === "stag");

    if (isEnabled) return <EndpointPurgeButton refreshEndpoint={this.refreshEndpoint} stopLoading={this.stopLoading} startLoading={this.startLoading} endpointId={endpointId} />;
    else return <></>;
  };

  buildSubscribeButton = (endpoint: string): JSX.Element => {
    return <EndpointAlertsButton endpointId={endpoint} />;
  };

  buildDisableButton = (endpointId: string, status: string): JSX.Element => {
    return <EndpointDisableButton refreshEndpoint={this.refreshEndpoint} stopLoading={this.stopLoading} startLoading={this.startLoading} endpointId={endpointId} status={status} />;
  };

  buildDisableHeartbeatButton = (endpointId: string, isHeartbeatEnabled: boolean | undefined): JSX.Element => {
    return <EndpointDisableHeartbeatButton refreshEndpoint={this.refreshEndpoint} stopLoading={this.stopLoading} startLoading={this.startLoading} endpointId={endpointId} isHeartbeatEnabled={isHeartbeatEnabled} />;
  };

  mapStatusToIcon = (status: EndpointStatus): JSX.Element => {
    const iconColor = mapStatusToColor(status);
    let iconName = "";

    if (status === EndpointStatus.Failed) {
      iconName = "cancel";
    } else if (status === EndpointStatus.Impacted) {
      iconName = "remove_circle";
    } else if (status === EndpointStatus.Pending) {
      iconName = "watch_later";
    } else if (status === EndpointStatus.Healthy) {
      iconName = "check_circle";
    } else if (status === EndpointStatus.Disabled) {
      iconName = "offline_bolt";
    }

    return (
      <Tooltip hasArrow={true} label={status} placement="top" aria-label={status}>
        <Box as={Icon} color={`${iconColor}.500`}>
          {iconName}
        </Box>
      </Tooltip>
    );
  };

  mapHeartbeatStatusToIcon = (status: HeartbeatStatus): JSX.Element => {
    const iconColor = mapHeartbeatStatusToColor(status);
    let iconName = "power_settings_new";

    /* switch (status) {
      case HeartbeatStatus.On:
        iconName =power_settings_new
        break;
      case HeartbeatStatus.Off:
        iconName =
        break;
      case HeartbeatStatus.Pending:
        iconName =
        break;
      case HeartbeatStatus.Unknown:
        iconName =
        break;
    } */

    return (
      <Tooltip hasArrow={true} label={status} placement="top" aria-label={status}>
        <Box as={Icon} color={`${iconColor}.500`}>
          {iconName}
        </Box>
      </Tooltip>
    );
  };
}
