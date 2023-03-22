import { Button, IconButton, Modal, ModalBody, ModalCloseButton, ModalContent, ModalFooter, ModalHeader, ModalOverlay, Tooltip, useDisclosure } from "@chakra-ui/react";
import React from "react";
import * as api from "api-client";
import { CheckIcon, CloseIcon } from "@chakra-ui/icons"

interface IEndpontDisableHeartbeatButtonProps {
    endpointId: string,
    isHeartbeatEnabled: boolean | undefined,
    refreshEndpoint: (endpointId: string) => {},
    startLoading: () => void,
    stopLoading: () => void
}

export default function EndpointDisableHeartbeatButton(props: IEndpontDisableHeartbeatButtonProps) {
    const client = new api.Client(api.CookieAuth());
    const { isOpen, onOpen, onClose } = useDisclosure();

    return (<>
        {props.isHeartbeatEnabled != undefined && <Tooltip label={(props.isHeartbeatEnabled ? "Disable" : "Enable") + " heartbeat on " + props.endpointId}>
            <IconButton size="sm" aria-label={(props.isHeartbeatEnabled ? "Disable" : "Enable") + " heartbeat on " + props.endpointId}
                icon={props.isHeartbeatEnabled ? <CloseIcon /> : <CheckIcon />}
                colorScheme={props.isHeartbeatEnabled ? "red" : "green"}
                onClick={(event) => {
                    event.preventDefault();
                    event.stopPropagation();
                    onOpen()
                }} />
        </Tooltip>}
        <Modal isOpen={isOpen} onClose={onClose} isCentered={true}>
            <ModalOverlay />
            <ModalContent>
                <ModalHeader>{(props.isHeartbeatEnabled ? "Disable" : "Enable") + " heartbeat on "}  {props.endpointId}?</ModalHeader>
                <ModalCloseButton />
                <ModalBody>
                    Are your sure you want to {(props.isHeartbeatEnabled ? "disable " : "enable")} heartbeat functionality on {props.endpointId}?
                </ModalBody>

                <ModalFooter>
                    <Button colorScheme="red" onClick={(event) => {
                        props.startLoading();
                        client.endpointEnableHeartbeat(props.endpointId, !props.isHeartbeatEnabled)
                            .then(result => {
                                props.refreshEndpoint(props.endpointId);
                            })
                            .catch(err => {
                                props.stopLoading();
                            })
                        onClose();
                    }}>Update</Button>
                </ModalFooter>
            </ModalContent>
        </Modal>
    </>)
}

