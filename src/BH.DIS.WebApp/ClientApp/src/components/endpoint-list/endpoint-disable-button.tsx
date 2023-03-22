import { Button, IconButton, Modal, ModalBody, ModalCloseButton, ModalContent, ModalFooter, ModalHeader, ModalOverlay, Tooltip, useDisclosure } from "@chakra-ui/react";
import React from "react";
import * as api from "api-client";
import { CheckIcon, CloseIcon } from "@chakra-ui/icons"

interface IEndpontDisableButtonProps {
  endpointId: string,
  status: string,
  refreshEndpoint: (endpointId: string) => {},
  startLoading: () => void,
  stopLoading: () => void
}

export default function EndpointDisableButton(props: IEndpontDisableButtonProps) {
  const client = new api.Client(api.CookieAuth());
  const { isOpen, onOpen, onClose } = useDisclosure();

  return (<>
    <Tooltip label={"Disable " + props.endpointId}>
      <IconButton size="sm" aria-label="Disable endpoint" icon={props.status === "enable" ? <CheckIcon /> : <CloseIcon />} colorScheme={props.status === "enable" ? "green" : "red"} onClick={(event) => {
        event.preventDefault();
        event.stopPropagation();
        onOpen()
      }} />
    </Tooltip>
    <Modal isOpen={isOpen} onClose={onClose} isCentered={true}>
      <ModalOverlay />
      <ModalContent>
        <ModalHeader>{props.status} {props.endpointId}?</ModalHeader>
        <ModalCloseButton />
        <ModalBody>
          Are your sure you want to {props.status}  {props.endpointId}?
        </ModalBody>

        <ModalFooter>
          <Button colorScheme="red" onClick={(event) => {
            props.startLoading();
            client.postEndpointSubscriptionstatus(props.endpointId, props.status).then(result => {
              props.refreshEndpoint(props.endpointId);
            })
              .catch((err) => {
                props.stopLoading();
              });
            onClose();
          }}>Update</Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  </>)
}

