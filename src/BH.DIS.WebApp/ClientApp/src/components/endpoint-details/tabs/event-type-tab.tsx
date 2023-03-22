import * as React from "react";
import * as api from "api-client";
import { RawNodeDatum } from "react-d3-tree/lib/types/common";

import {
    Code,
    Collapse,
    Heading,
    Divider,
    Textarea,
    Select,
    Flex,
    Modal,
    ModalOverlay,
    ModalContent,
    ModalHeader,
    ModalFooter,
    ModalBody,
    ModalCloseButton,
    Button,
    ButtonGroup,
    useDisclosure,
    Text,
    Link,
    Stack,
    Box,
    Switch,
    FormLabel,
    FormControl
} from "@chakra-ui/react";
import { ComposeNewResponse } from "pages/endpoint-details";
import { View, EventTree } from "components/event-details/event-tree";
import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import EventTypeGroupingOverview from "../eventType-grouping-overview";

interface IEventTypeTabProps {
    pageDimensions: { width: number, height: number };
}


const EventTypeTab = (props: IEventTypeTabProps) => {
    const client = new api.Client(api.CookieAuth());
    const params = useParams();

    const [consumerChart, setConsumerChart] = React.useState<RawNodeDatum>();
    const [producerChart, setProducerChart] = React.useState<RawNodeDatum>();

    const [eventTypeDetails, setEventTypeDetails] = React.useState<api.Anonymous>();

    const [newEventText, setNewEventText] = React.useState<string>("");
    const [eventTreeView, setEventTreeView] = React.useState<View>(View.Produces);

    const [composeNewModalIsOpen, setComposeNewModalIsOpen] = React.useState<boolean>(false);
    const [composeNewModalResponse, setComposeNewModalResponse] = React.useState<ComposeNewResponse>({hasError: false, responseString: ""});
    const [activeEventType, setActiveEventTypeHook] = React.useState<api.EventType | undefined>(undefined)

    React.useEffect(() => {
        const fetchData = async () => {
            const result = await client.getEventtypesByEndpointId(params.id!)
            setEventTypeDetails(result);
        }
        fetchData()
    }, []);

    React.useEffect(() => {
        setConsumerChart(getProduces())
        setProducerChart(getConsumes())
    }, [eventTypeDetails]);

    const getConsumes = (): RawNodeDatum => {
        let i = 0;
        const consumes: RawNodeDatum = {
            name: params.id!.toLowerCase(),
            children: [],
        }

        eventTypeDetails?.eventTypeDetails?.filter((e) => e.producers?.some(c => c.toLowerCase() === params.id!.toLowerCase())).forEach((eventType) => {

            let j = 0;
            consumes.children![i] = {
                name: eventType.eventType?.id!.toLowerCase()!,
                children: []
            }
            eventType.consumers?.forEach((c) => {
                consumes.children![i].children![j] = {
                    name: c.toLowerCase()
                }
                j++;
            });
            i++;
        })
        return consumes;

    }

    const getProduces = (): RawNodeDatum => {
        let i = 0;
        const produces: RawNodeDatum = {
            name: params.id!.toLowerCase(),
            children: [],
        }

        eventTypeDetails?.eventTypeDetails?.filter((e) => e.consumers?.some(c => c.toLowerCase() === params.id!.toLowerCase())).forEach((eventType) => {

            let j = 0;
            produces.children![i] = {
                name: eventType.eventType?.id!.toLowerCase()!,
                children: []
            }
            eventType.producers?.forEach((c) => {
                produces.children![i].children![j] = {
                    name: c.toLowerCase()
                }
                j++;
            });
            i++;
        })
        return produces;
    }

    const handleInputChange = (e: any) => {
        const inputValue = e.target.value;
        setNewEventText(inputValue);
    };

    const setActiveEventType = async (eventType: api.EventType) => {
        const object: any = {};
        eventType.properties?.forEach((e) => {
            if (e.name != null && e.name !== "MessageMetadata") {
                object[e.name] = e.typeName;
            }
        });
        const json = JSON.stringify(object, null, 2);
        setActiveEventTypeHook(eventType);
        setComposeNewModalIsOpen(true);
        setNewEventText(json);
    };

    const setModalState = async (state: boolean) => {
        setComposeNewModalIsOpen(state);
        setComposeNewModalResponse({ hasError: false, responseString: "" });
    };

    const postComposeNewEvent = () => {
        const body = new api.ResubmitWithChanges();
        body.eventTypeId = activeEventType!.id;
        body.eventContent = newEventText;
        client
            .postComposeNewEvent(body)
            .then(async (res) => {
                setComposeNewModalResponse({
                    hasError: false,
                    responseString: "Event Composed successfully.",
                })
            })
            .catch(async (res) => {
                // Feedback to user that event was malformed
                setComposeNewModalResponse({
                    hasError: true,
                    responseString: `Invalid event was not composed: ${res.response!}`,
                });
            })
            .finally(() => {
                setTimeout(async () => { setComposeNewModalResponse({ hasError: false, responseString: "" }); }, 4000);
            });
    };

    return (
        <Box height="100%" width="100%" >
            <Stack width="100%" isInline={true} shouldWrapChildren={true} justify="space-around" borderBottom="1px solid #dee2e6" >
                <Box>
                    <Heading>Produces</Heading>
                    {eventTypeDetails?.produces?.map(x => <EventTypeGroupingOverview
                        key={x.namespace!}
                        namespace={x.namespace!}
                        show={false}
                        events={x.events!}
                        triggerHandler={setActiveEventType}
                    />)}
                </Box>
                <Box>
                    <Heading>Consumes</Heading>
                    {eventTypeDetails?.consumes?.map(x =>
                        <EventTypeGroupingOverview
                            key={x.namespace!}
                            namespace={x.namespace!}
                            show={false}
                            events={x.events!}
                            triggerHandler={setActiveEventType}
                        />)}
                </Box>
                {activeEventType !== undefined ?
                    <Modal
                        isOpen={composeNewModalIsOpen}
                        onClose={() => setModalState(false)}
                        size="xl"
                    >
                        <ModalOverlay />
                        <ModalContent minH="400px">
                            <ModalHeader>Trigger new event</ModalHeader>
                            <ModalCloseButton />
                            <ModalBody>
                                Event Type: {activeEventType?.name}
                                <br />
                                <Textarea
                                    minH="350px"
                                    value={newEventText}
                                    onChange={handleInputChange}
                                    size="sm"
                                />
                                <Text color={composeNewModalResponse?.hasError ? "red.600" : "green.400"}>{composeNewModalResponse?.responseString}</Text>
                            </ModalBody>

                            <ModalFooter>
                                <Button
                                    colorScheme="blue"
                                    mr={4}
                                    variant="outline"
                                    onClick={(e) => setModalState(false)}
                                >
                                    Close
                                </Button>
                                <Button isDisabled={composeNewModalResponse.hasError} onClick={postComposeNewEvent}>Send event</Button>
                            </ModalFooter>
                        </ModalContent>
                    </Modal>
                    : <></>}

            </Stack>
            <Stack spacing="24px">
                <Stack width="20%" style={{ display: "flex", alignItems: "center" }} >
                    <FormControl>
                        <Stack direction="column">
                            <FormLabel>Produces or Consumes?</FormLabel>
                            <Switch size="lg" color="teal" onChange={(e) => {
                                if (eventTreeView === View.Consumes)
                                    setEventTreeView(View.Produces);
                                else
                                    setEventTreeView(View.Consumes);
                            }} />
                        </Stack>
                    </FormControl>
                </Stack>
                <Box>
                    {consumerChart && producerChart ? <EventTree
                        translate={{ x: props.pageDimensions.width / 2, y: 25 }}
                        pageDimensions={props.pageDimensions}
                        data={eventTreeView === View.Produces ? producerChart : consumerChart}
                        view={eventTreeView} /> : <></>}
                </Box>
            </Stack>
        </Box>
    );
}

export default EventTypeTab;
