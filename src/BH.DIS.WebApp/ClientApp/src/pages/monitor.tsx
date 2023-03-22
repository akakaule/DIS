import { Box } from "@chakra-ui/react";
import * as signalR from "@microsoft/signalr";
import * as api from "api-client";
import Dashboard from "components/live-monitor/dashboard";
import * as moment from "moment";
import * as React from "react";

type MonitorProps = { endpointStates: api.EndpointStatusCount[]; heartbeatDict: api.MetadataShort[] };
type MonitorState = { endpointStates: api.EndpointStatusCount[]; heartbeatDict: api.MetadataShort[] };

export default class Monitor extends React.Component<MonitorProps, MonitorState> {
  private hubConnection: signalR.HubConnection;
  private client: api.Client;

  constructor(props: MonitorProps) {
    super(props);

    const mappedProps = this.props.endpointStates?.map((x) => this.mapMomentInEndpointStatus(x));
    this.state = { endpointStates: mappedProps, heartbeatDict: props.heartbeatDict };

    this.handleEvent = this.handleEvent.bind(this);
  }

  // componentDidMount gets called once we're client-side
  async componentDidMount() {
    this.client = new api.Client(api.CookieAuth());

    this.hubConnection = new signalR.HubConnectionBuilder().withUrl("/hubs/gridevents").withAutomaticReconnect().configureLogging(signalR.LogLevel.Information).build();

    // If we didn't rehydrate with endpoint states, call the api to get them
    const endpoints = this.props.endpointStates ? this.props.endpointStates?.map((x) => api.EndpointStatus.fromJS(x)) : await this.client.getEndpointStatusCountAll();

    const transformedStates = this.transformStates(endpoints);
    let endpointIds: string[] = [];
    endpointIds = transformedStates.map((x) => {
      return x.endpointId!;
    });

    let heartbeatsDict: api.MetadataShort[] = [];
    try {
      heartbeatsDict = await this.client.postApiMetadatashort(endpointIds);
    } catch (error) {}

    this.hubConnection.start().catch((err: any) => console.error(err.toString()));
    this.hubConnection.on("endpointupdate", this.handleEvent);
    this.hubConnection.on("heartbeatupdate", this.handleHeartbeat);

    this.setState({ ...this.state, endpointStates: transformedStates, heartbeatDict: heartbeatsDict });
  }

  handleEvent(event: api.EndpointStatusCount) {
    const initializedEvent = this.mapMomentInEndpointStatus(event);
    const clone = [...this.state.endpointStates];
    const idx = clone.findIndex((x) => x.endpointId?.toLowerCase() === initializedEvent.endpointId?.toLowerCase());
    if (idx > -1) {
      clone[idx] = initializedEvent;
    } else {
      clone.push(api.EndpointStatusCount.fromJS(initializedEvent));
    }
    this.setState({ ...this.state, endpointStates: clone });
  }

  handleHeartbeat(heartbeat: api.MetadataShort) {
    const clone = [...this.state.heartbeatDict];
    const idx = clone.findIndex((x) => x.endpointId?.toLowerCase() === heartbeat.endpointId?.toLowerCase());
    if (idx > -1) {
      clone[idx] = heartbeat;
    } else {
      clone.push(heartbeat);
    }
    this.setState({ ...this.state, heartbeatDict: clone });
  }

  transformStates(endpoints: api.EndpointStatusCount[]): api.EndpointStatusCount[] {
    const transformedStates = [...(endpoints ?? [])];
    transformedStates.forEach((x) => {
      x.eventTime = x.eventTime ?? moment();
    });
    return transformedStates;
  }

  mapMomentInEndpointStatus(endpointStatus: api.EndpointStatusCount) {
    return api.EndpointStatusCount.fromJS(endpointStatus);
  }

  render() {
    return (
      <Box bg="gray.700">
        <Dashboard title="Live Status Monitor" cards={this.state.endpointStates} heartbeatDict={this.state.heartbeatDict} />
      </Box>
    );
  }
}
