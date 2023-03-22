import { AddIcon, DeleteIcon } from "@chakra-ui/icons";
import { Button, IconButton, Input, Modal, ModalBody, ModalCloseButton, ModalContent, ModalFooter, ModalHeader, ModalOverlay, Table, TableContainer, Tbody, Td, Text, Th, Thead, Tr, useDisclosure } from "@chakra-ui/react";
import * as api from "api-client";
import React, { Dispatch, SetStateAction, useState } from "react";
import { useParams } from "react-router-dom";

interface IInformationButtonProps {
  metaData: api.Metadata | undefined;
  icon: React.ReactElement<any, string | React.JSXElementConstructor<any>> | undefined;
  onClose: Dispatch<SetStateAction<api.Metadata | undefined>>;
  buttonText: string;
}

export default function MetadataButton(props: IInformationButtonProps) {
  const client = new api.Client(api.CookieAuth());
  const params = useParams();
  const [endpointOwnerTeam, setendpointOwnerTeam] = useState(props.metaData && props.metaData.endpointOwnerTeam ? props.metaData.endpointOwnerTeam : "");
  const [endpointOwner, setEndpointOwner] = useState(props.metaData && props.metaData.endpointOwner ? props.metaData.endpointOwner : "");
  const [endpointOwnerEmail, setEndpointOwnerEmail] = useState(props.metaData && props.metaData.endpointOwnerEmail ? props.metaData.endpointOwnerEmail : "");

  const [technicalContacts, setTechnicalContacts] = useState<api.TechnicalContact[]>(props.metaData && props.metaData.technicalContacts ? props.metaData.technicalContacts : []);

  const [newTechnicalContactName, setNewTechnicalContactName] = useState("");
  const [newTechnicalContactEmail, setNewTechnicalContactEmail] = useState("");

  const { isOpen, onOpen, onClose } = useDisclosure();
  const [feedback, setFeedback] = useState("");
  const [feedbackColour, setFBcolour] = useState("Green");
  const [validation, setValidation] = useState(true);

  React.useEffect(() => {
    validate();
  }, [endpointOwner, endpointOwnerTeam, technicalContacts, endpointOwnerEmail]);

  const handleOwnerInputChange = (e: React.FormEvent<HTMLInputElement>) => {
    setEndpointOwner(e.currentTarget.value);
  };

  const handleOwnerTeamInputChange = (e: React.FormEvent<HTMLInputElement>) => {
    setendpointOwnerTeam(e.currentTarget.value);
  };

  const handleOwnerEmailInputChange = (e: React.FormEvent<HTMLInputElement>) => {
    setEndpointOwnerEmail(e.currentTarget.value);
  };

  const handlenewTechnicalContactNameInputChange = (e: React.FormEvent<HTMLInputElement>) => {
    setNewTechnicalContactName(e.currentTarget.value);
  };

  const handlenewTechnicalContactEmailInputChange = (e: React.FormEvent<HTMLInputElement>) => {
    setNewTechnicalContactEmail(e.currentTarget.value);
  };

  const handleTechnicalContactNameChange = (e: React.FormEvent<HTMLInputElement>, index: number) => {
    if (index !== -1) {
      const nextTechnicalContacts = [...technicalContacts];
      const contact = nextTechnicalContacts[index];
      contact.name = e.currentTarget.value;
      setTechnicalContacts(nextTechnicalContacts);
    } else {
      const contact = new api.TechnicalContact();
      contact.name = e.currentTarget.value;
      setTechnicalContacts([...technicalContacts, contact]);
    }
  };

  const handleTechnicalContactEmailChange = (e: React.FormEvent<HTMLInputElement>, index: number) => {
    if (index !== -1) {
      const nextTechnicalContacts = [...technicalContacts];
      const contact = nextTechnicalContacts[index];
      contact.email = e.currentTarget.value;
      setTechnicalContacts(nextTechnicalContacts);
    } else {
      const contact = new api.TechnicalContact();
      contact.email = e.currentTarget.value;
      setTechnicalContacts([...technicalContacts, contact]);
    }
  };

  function validate() {
    const pattern = new RegExp(
      /^(("[\w-\s]+")|([\w-]+(?:\.[\w-]+)*)|("[\w-\s]+")([\w-]+(?:\.[\w-]+)*))(@((?:[\w-]+\.)*\w[\w-]{0,66})\.([a-z]{2,6}(?:\.[a-z]{2})?)$)|(@\[?((25[0-5]\.|2[0-4][0-9]\.|1[0-9]{2}\.|[0-9]{1,2}\.))((25[0-5]|2[0-4][0-9]|1[0-9]{2}|[0-9]{1,2})\.){2}(25[0-5]|2[0-4][0-9]|1[0-9]{2}|[0-9]{1,2})\]?$)/i
    );
    let technicalContactValidation = false;
    if (technicalContacts.length !== 0) {
      technicalContacts.forEach((technicalContact) => {
        technicalContactValidation = validateTechnicalContact(technicalContact.name, technicalContact.email);
        if (!technicalContactValidation) {
          return;
        }
      });
    } else {
      technicalContactValidation = true;
    }

    if (endpointOwner === "" || endpointOwnerTeam === "" || !pattern.test(endpointOwnerEmail) || !technicalContactValidation) {
      setValidation(false);
    } else {
      setValidation(true);
    }
  }

  function validateTechnicalContact(name: string | undefined, email: string | undefined) {
    const pattern = new RegExp(
      /^(("[\w-\s]+")|([\w-]+(?:\.[\w-]+)*)|("[\w-\s]+")([\w-]+(?:\.[\w-]+)*))(@((?:[\w-]+\.)*\w[\w-]{0,66})\.([a-z]{2,6}(?:\.[a-z]{2})?)$)|(@\[?((25[0-5]\.|2[0-4][0-9]\.|1[0-9]{2}\.|[0-9]{1,2}\.))((25[0-5]|2[0-4][0-9]|1[0-9]{2}|[0-9]{1,2})\.){2}(25[0-5]|2[0-4][0-9]|1[0-9]{2}|[0-9]{1,2})\]?$)/i
    );

    if (!name || !email || name === "" || email === "" || !pattern.test(email)) {
      return false;
    } else {
      return true;
    }
  }

  const clearAlertsFeedback = () => {
    setFeedback("");
  };

  const editEndpointMetadata = () => {
    const body = new api.Metadata({
      id: params.id!,
      endpointOwner: endpointOwner,
      endpointOwnerTeam: endpointOwnerTeam,
      endpointOwnerEmail: endpointOwnerEmail,
      technicalContacts: technicalContacts,
    });
    client
      .postMetadataEndpoint(params.id!, body)
      .then((r) => {
        props.onClose(body);
        setFeedback("Successfully added metadata to " + params.id!);
        // setTimeout(onClose,4000);
      })
      .catch((r) => {
        setFeedback("Unable to add metadata. " + r);
        setFBcolour("Tomato");
      })
      .finally(() => {
        setTimeout(clearAlertsFeedback, 4000);
      });
  };

  function addTechnicalContact() {
    if (newTechnicalContactName === "") {
      setFeedback("New contact name cannot be empty");
      setFBcolour("Tomato");
      setTimeout(clearAlertsFeedback, 4000);
      return;
    }
    const pattern = new RegExp(
      /^(("[\w-\s]+")|([\w-]+(?:\.[\w-]+)*)|("[\w-\s]+")([\w-]+(?:\.[\w-]+)*))(@((?:[\w-]+\.)*\w[\w-]{0,66})\.([a-z]{2,6}(?:\.[a-z]{2})?)$)|(@\[?((25[0-5]\.|2[0-4][0-9]\.|1[0-9]{2}\.|[0-9]{1,2}\.))((25[0-5]|2[0-4][0-9]|1[0-9]{2}|[0-9]{1,2})\.){2}(25[0-5]|2[0-4][0-9]|1[0-9]{2}|[0-9]{1,2})\]?$)/i
    );

    if (!pattern.test(newTechnicalContactEmail)) {
      setFeedback("New contact email is not valid");
      setFBcolour("Tomato");
      setTimeout(clearAlertsFeedback, 4000);
      return;
    }

    const contact = new api.TechnicalContact();
    contact.name = newTechnicalContactName;
    contact.email = newTechnicalContactEmail;
    setTechnicalContacts([...technicalContacts, contact]);
    setNewTechnicalContactName("");
    setNewTechnicalContactEmail("");
  }

  return (
    <>
      <Button
        aria-label="Subscribe to alerts on endpoint"
        leftIcon={props.icon}
        colorScheme="yellow"
        onClick={(event) => {
          event.preventDefault();
          event.stopPropagation();
          onOpen();
        }}
      >
        {props.buttonText}
      </Button>
      <Modal isOpen={isOpen} onClose={onClose} isCentered={true} size={"5xl"}>
        <ModalOverlay />
        <ModalContent>
          <ModalHeader>
            {" "}
            {props.metaData ? "Update metadata on" : "Add metadata to"} {params.id!}
          </ModalHeader>
          <ModalCloseButton />
          <ModalBody>
            <Text>Fill out endpoint owner, Technical contacts name and email.</Text>
            <br />
            <Text fontWeight="bold">Owner team</Text>
            <Input placeholder="John Smith" value={endpointOwnerTeam} onChange={handleOwnerTeamInputChange} />
            <br />
            <Text fontWeight="bold">Owner (PO)</Text>
            <Input placeholder="John Smith" value={endpointOwner} onChange={handleOwnerInputChange} />
            <br />
            <Text fontWeight="bold">Owner email (PO)</Text>
            <Input placeholder="john@smith.com" value={endpointOwnerEmail} onChange={handleOwnerEmailInputChange} />
            <br />

            <TableContainer>
              <Table variant="simple">
                <Thead>
                  <Tr>
                    <Th colSpan={3} textAlign="center" border={"0px"} textColor="black">
                      Technical Contacts
                    </Th>
                  </Tr>
                  <Tr>
                    <Th>Name</Th>
                    <Th>Email</Th>
                    <Th> Add/Delete</Th>
                  </Tr>
                </Thead>
                <Tbody>
                  {technicalContacts?.map((technicalContact, idx) => (
                    <Tr key={idx}>
                      <Td key={idx + "namecolmodal"}>
                        <Input key={idx + "name"} value={technicalContact.name} onChange={(e) => handleTechnicalContactNameChange(e, idx)} />
                      </Td>
                      <Td key={idx + "emailcolmodal"}>
                        <Input key={idx + "email"} value={technicalContact.email} onChange={(e) => handleTechnicalContactEmailChange(e, idx)} />
                      </Td>
                      <Td>
                        <IconButton
                          icon={<DeleteIcon />}
                          aria-label="Delete element from list"
                          onClick={() => {
                            setTechnicalContacts(technicalContacts.filter((c) => c !== technicalContact));
                          }}
                        />
                      </Td>
                    </Tr>
                  ))}
                  <Tr>
                    <Td>
                      <Input placeholder="Name" value={newTechnicalContactName} onChange={handlenewTechnicalContactNameInputChange}></Input>
                    </Td>
                    <Td>
                      <Input placeholder="Email" value={newTechnicalContactEmail} onChange={handlenewTechnicalContactEmailInputChange}></Input>
                    </Td>
                    <Td>
                      <IconButton icon={<AddIcon />} aria-label="Add new technical contact" onClick={addTechnicalContact} />
                    </Td>
                  </Tr>
                </Tbody>
              </Table>
            </TableContainer>
            <br />
            <Text as="i" color={feedbackColour}>
              {feedback}
            </Text>
          </ModalBody>
          <ModalFooter>
            <Button
              colorScheme="yellow"
              isDisabled={!validation}
              onClick={(event) => {
                editEndpointMetadata();
              }}
            >
              {props.metaData ? "Update metadata" : "Add metadata"}
            </Button>
          </ModalFooter>
        </ModalContent>
      </Modal>
    </>
  );
}
