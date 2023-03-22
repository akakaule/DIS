// import { Button, IconButton, Modal, ModalBody, ModalCloseButton, ModalContent, ModalFooter, ModalHeader, ModalOverlay, useDisclosure } from "@chakra-ui/react";
// import React from "react";
// import * as api from "api-client";

// interface  ISessionPurgeButtonProps{
//   endpointId: string,
//   sessionId: string
// }

// export default function SessionPurgeButton(props: ISessionPurgeButtonProps) {
//   var client = new api.Client(api.CookieAuth());
//   const { isOpen, onOpen, onClose } = useDisclosure();

//   return (<><IconButton aria-label="Purge session" icon="delete" variantColor="red" onClick={(event) => {
//     event.preventDefault();
//     event.stopPropagation();
//     onOpen()
//   }}></IconButton>
//   <Modal isOpen={isOpen} onClose={onClose} isCentered>
//         <ModalOverlay />
//         <ModalContent>
//           <ModalHeader>Purge {props.endpointId}?</ModalHeader>
//           <ModalCloseButton />
//           <ModalBody>
//             Are your sure you want to delete all Failed, Deferred and Pending messages in {props.sessionId} on {props.endpointId} ?
//           </ModalBody>

//           <ModalFooter>
//             <Button colorScheme="red" onClick={(event) => {
//               client.postEndpointPurgeSessionId(props.endpointId, props.sessionId);
//               onClose(); 
//             }}>Purge</Button>
//           </ModalFooter>
//         </ModalContent>
//       </Modal>
//     </>)
// }

