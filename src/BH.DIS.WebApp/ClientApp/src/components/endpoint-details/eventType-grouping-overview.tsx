import { Button, Collapse, Divider, Link } from "@chakra-ui/react";
import * as React from "react";
import * as api from "api-client";
import { useState } from "react";

interface IEventTypeGroupingOverviewProps {
    show: boolean;
    namespace: string;
    events: api.EventType[];
    triggerHandler: (event: api.EventType) => void;
}

const EventTypeGroupingOverview = (props: IEventTypeGroupingOverviewProps) => {
    const [show, setShow] = React.useState(props.show);

    if (!props.events || props.events?.length! < 1)
        return <div id={props.namespace}>"No events"</div>
    else
        return (
            <div id={props.namespace}>
                <b>{props.namespace}</b>{" "}
                <Button size="xs" colorScheme="blue" onClick={() => setShow(!show)}>
                    {show ? "Hide events" : "Show events"}
                </Button>
                <Divider />
                <Collapse in={show}>
                    {props.events?.map((e) => {
                        return (
                            <p key={e.id}>
                                <Link href={"/EventTypes/Details/" + e.id} color="blue.500">{e.id}</Link>
                                <Button
                                    size="xs"
                                 onClick={(ev) => props.triggerHandler(e)}
                                >
                                    Trigger event
                                </Button>
                            </p>
                        );
                    })}
                    <Divider />
                </Collapse>
            </div>)
}

export default EventTypeGroupingOverview;