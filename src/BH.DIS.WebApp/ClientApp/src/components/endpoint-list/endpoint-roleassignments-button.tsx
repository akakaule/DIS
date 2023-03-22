import { Button, Modal, ModalBody, ModalCloseButton, ModalContent, ModalFooter, ModalHeader, ModalOverlay, useDisclosure, Textarea, useClipboard } from "@chakra-ui/react";
import React from "react";

interface IEndpointRoleAssignmentsButtonProps {
  endpointId: string,
  script: string
}


export default function EndpointRoleAssignmentsButton(props: IEndpointRoleAssignmentsButtonProps) {

  const { isOpen, onOpen, onClose } = useDisclosure();
  const { hasCopied, onCopy } = useClipboard(props.script)
 

  return (<><Button paddingLeft="1" paddingRight="1" size="sm" aria-label="Get Role Assignments" colorScheme="green" onClick={(event) => {
    
    event.preventDefault();
    event.stopPropagation();
    onOpen()
  }}>&lt;/&gt;</Button>
    <Modal size="full" isOpen={isOpen} onClose={onClose} isCentered={true}>
      <ModalOverlay />
      <ModalContent>
        <ModalHeader>Role Assignment script for {props.endpointId}</ModalHeader>
        <ModalCloseButton />
        <ModalBody>
          <Textarea defaultValue={props.script} isReadOnly={true} color="gray.500" height="600px" />
        </ModalBody>
        <ModalFooter >
          <Button onClick={onCopy} ml={2}>
            {hasCopied ? "Copied" : "Copy"}
          </Button>
          &nbsp;
          <Button onClick={onClose}>Close</Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  </>)
}