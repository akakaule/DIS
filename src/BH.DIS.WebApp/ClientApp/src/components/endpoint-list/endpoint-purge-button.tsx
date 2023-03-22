import { Button, IconButton, Modal, ModalBody, ModalCloseButton, ModalContent, ModalFooter, ModalHeader, ModalOverlay, Tooltip, useDisclosure } from "@chakra-ui/react";
import React from "react";
import * as api from "api-client";
import { DeleteIcon } from "@chakra-ui/icons";

interface IEndpontPurgeButtonProps {
  endpointId: string,
  refreshEndpoint: (endpointId: string) => {},
  startLoading: () => void,
  stopLoading: () => void
}

export default function EndpointPurgeButton(props: IEndpontPurgeButtonProps) {
  const client = new api.Client(api.CookieAuth());
  const { isOpen, onOpen, onClose } = useDisclosure();

  return (<>
    <Tooltip label={"Purge everything on " + props.endpointId}>
      <IconButton size="sm" aria-label="Purge endpoint" icon={<DeleteIcon />} colorScheme="red" onClick={(event) => {
        event.preventDefault();
        event.stopPropagation();
        onOpen()
      }} />
    </Tooltip>
    <Modal isOpen={isOpen} onClose={onClose} isCentered={true}>
      <ModalOverlay />
      <ModalContent>
        <ModalHeader>Purge {props.endpointId}?</ModalHeader>
        <ModalCloseButton />
        <ModalBody>
          Are your sure you want to delete all Failed, Deferred and Pending messages on {props.endpointId}?
        </ModalBody>

        <ModalFooter>
          <Button colorScheme="red" onClick={(event) => {
            props.startLoading();
            client.postEndpointPurge(props.endpointId).then(result => {
              props.refreshEndpoint(props.endpointId);
            })
              .catch((err) => {
                props.stopLoading();
              });
            onClose();
          }}>Purge</Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  </>)
}

