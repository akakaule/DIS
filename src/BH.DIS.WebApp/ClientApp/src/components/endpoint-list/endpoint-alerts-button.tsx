import {
  Button,
  IconButton,
  Modal,
  ModalBody,
  ModalCloseButton,
  ModalContent,
  ModalFooter,
  ModalHeader,
  ModalOverlay,
  useDisclosure,
  Input,
  Radio,
  RadioGroup,
  Stack,
  Text,
  Checkbox,
  CheckboxGroup,
  Switch,
  FormControl,
  FormLabel,
  Link,
  Accordion,
  AccordionItem,
  AccordionButton,
  AccordionIcon,
  Box,
  AccordionPanel,
  Grid,
  GridItem,
  Tooltip,
} from "@chakra-ui/react";
import React, { FormEvent, RefAttributes, useEffect, useRef, useState } from "react";
import * as api from "api-client";
import { BellIcon } from "@chakra-ui/icons";
import { CUIAutoComplete } from "chakra-ui-autocomplete";
import { TextField } from "@material-ui/core";

interface IEndpontAlertsButtonProps {
  endpointId: string;
}

export interface Item {
  label: string;
  value: string;
}

export default function EndpointAlertsButton(props: IEndpontAlertsButtonProps) {
  const client = new api.Client(api.CookieAuth());
  const [email, setEmail] = useState("");
  const [url, setUrl] = useState("");
  const { isOpen, onOpen, onClose } = useDisclosure();
  const [feedback, setFeedback] = useState("");
  const [notificationType, setNotificationType] = useState<string>("mail");
  const [feedbackColour, setFBcolour] = useState("Green");
  const [validation, setValidation] = useState(true);
  const [eventTypes, setEventTypes] = React.useState<Item[]>([]);
  const [selectedEventTypes, setSelectedEventTypes] = React.useState<Item[]>([]);
  const [payload, setPayload] = React.useState<string>();
  const [frequency, setFrequency] = React.useState<string>("86400");
  const dataLoaded = useRef(false);

  const getEventTypes = async () => {
    const fetchData = async () => {
      const result = await client.getEventtypesByEndpointId(props.endpointId);

      const consumes = result.consumes
        ?.map((event) => event.events!)
        .reduce((pre, cur) => pre.concat(cur), [])
        .map((event) => event.name!);

      const produces = result.produces
        ?.map((event) => event.events!)
        .reduce((pre, cur) => pre.concat(cur), [])
        .map((event) => event.name!);

      const tempEventTypes = [...consumes!, ...produces!];
      const eventTypesItems = tempEventTypes.map((x) => {
        return { label: x, value: x } as Item;
      });

      setEventTypes(eventTypesItems);

      // const tempCheckboxMap = new Map<string, boolean>();
      // tempEventTypes.forEach(x => tempCheckboxMap.set(x, false));
      // setCheckboxEventTypes(tempCheckboxMap);
    };
    fetchData();

    dataLoaded.current = true;
  };

  React.useEffect(() => {
    if (isOpen && !dataLoaded.current) {
      getEventTypes();
    }
  }, [isOpen]);

  React.useEffect(() => {
    validate();
  }, [notificationType, email, url]);

  const handleEmailInputChange = (e: React.FormEvent<HTMLInputElement>) => {
    setEmail(e.currentTarget.value);
  };
  const handleUrlInputChange = (e: React.FormEvent<HTMLInputElement>) => {
    setUrl(e.currentTarget.value);
  };

  function validate() {
    if (email) {
      const pattern = new RegExp(
        /^(("[\w-\s]+")|([\w-]+(?:\.[\w-]+)*)|("[\w-\s]+")([\w-]+(?:\.[\w-]+)*))(@((?:[\w-]+\.)*\w[\w-]{0,66})\.([a-z]{2,6}(?:\.[a-z]{2})?)$)|(@\[?((25[0-5]\.|2[0-4][0-9]\.|1[0-9]{2}\.|[0-9]{1,2}\.))((25[0-5]|2[0-4][0-9]|1[0-9]{2}|[0-9]{1,2})\.){2}(25[0-5]|2[0-4][0-9]|1[0-9]{2}|[0-9]{1,2})\]?$)/i
      );
      if (pattern.test(email) && notificationType === "mail") setValidation(false);
      else {
        setValidation(true);
      }

      if (url) {
        const pattern = new RegExp("[a-zA-Z0-9@:%._\\+~#?&//=]{2,256}\\.[a-z]{2,6}\\b([-a-zA-Z0-9@:%._\\+~#?&//=]*)");
        if (pattern.test(url) && notificationType == "teams") setValidation(false);
        else {
          setValidation(true);
        }
      }
    }
  }

  const clearAlertsFeedback = () => {
    setFeedback("");
  };

  const SubscribeToAlertsEndpoint = () => {
    const body = new api.EndpointSubscription({
      mail: email,
      type: notificationType,
      eventTypes: selectedEventTypes.map((x) => x.value),
      url,
      payload: payload,
      frequency: parseInt(frequency),
    });
    client
      .postEndpointSubscribe(props.endpointId, body)
      .then((r) => {
        setFeedback("Successfully subscribed to alerts on " + props.endpointId);
        // setTimeout(onClose,4000);
      })
      .catch((r) => {
        setFeedback("Unable to subscribe to alerts. " + r);
        setFBcolour("Tomato");
      })
      .finally(() => {
        setTimeout(clearAlertsFeedback, 4000);
      });
  };

  return (
    <>
      <Tooltip label={"Subscribe to alerts on " + props.endpointId}>
        <IconButton
          size="sm"
          aria-label="Subscribe to alerts on endpoint"
          icon={<BellIcon />}
          colorScheme="yellow"
          onClick={(event) => {
            event.preventDefault();
            event.stopPropagation();
            onOpen();
          }}
        />
      </Tooltip>
      <Modal isOpen={isOpen} onClose={onClose} isCentered={true}>
        <ModalOverlay />
        <ModalContent>
          <ModalHeader>Subscribe to alerts from {props.endpointId}</ModalHeader>
          <ModalCloseButton />
          <ModalBody>
            <Text>Fill out email or url, severity and type below in order to subscribe to alerts from EIP when {props.endpointId} is affected.</Text>
            <br />
            <Text as="i">
              Learn more about how to create a logic app with teams integration on our wiki
              <Link href="https://bhgprod.visualstudio.com/Integration/_wiki/wikis/Integration.wiki/336/Teams-integration-using-LogicApps"> here.</Link>
            </Text>
            <br />
            <br />
            <Text fontWeight="bold" hidden={!(notificationType === "teams")}>
              Url
            </Text>
            <Text fontWeight="bold" hidden={!(notificationType === "mail")}>
              Email
            </Text>
            <Input hidden={!(notificationType === "teams")} placeholder="https://prod-144.westeurope.logic.azure.com:443..." onChange={handleUrlInputChange} />
            <Input hidden={!(notificationType === "mail")} placeholder="example@email.com" onChange={handleEmailInputChange} />
            <br />
            <br />
            <Text fontWeight="bold">Frequency</Text>
            <RadioGroup onChange={setFrequency} value={frequency}>
              <Stack direction="row">
                <Radio value="3600">Hourly</Radio>
                <Radio value="86400">Daily</Radio>
                <Radio value="604800">Weekly</Radio>
              </Stack>
            </RadioGroup>
            <br />
            <Text fontWeight="bold">Type</Text>
            <RadioGroup onChange={setNotificationType} value={notificationType}>
              <Stack direction="row">
                <Radio value="mail">Mail</Radio>
                <Radio value="teams">Teams</Radio>
              </Stack>
            </RadioGroup>
            <br />
            <Text fontWeight="bold">Event Filtering</Text>
            <Box style={{}}>
              <CUIAutoComplete
                labelStyleProps={{ height: 0, margin: 0 }}
                listStyleProps={{ margin: 0 }}
                label=""
                placeholder="Type an event"
                items={eventTypes}
                selectedItems={selectedEventTypes}
                createItemRenderer={(value) => {
                  return (
                    <Text>
                      <Box as="span" bg="red.300" fontWeight="bold">
                        "{value}"
                      </Box>
                    </Text>
                  );
                }}
                onSelectedItemsChange={(changes) => {
                  if (selectedEventTypes) {
                    setSelectedEventTypes(changes.selectedItems!);
                  }
                }}
              />
            </Box>
            <TextField
              multiline={true}
              minRows={3}
              id="outlined-basic"
              label="Payload filter"
              variant="outlined"
              margin="none"
              style={{ paddingLeft: "16", width: "-webkit-fill-available", height: "-webkit-fill-available" }}
              onChange={(event: React.ChangeEvent<HTMLInputElement>) => {
                setPayload(event.target.value);
              }}
            />
            <br />
            <Text as="i" color={feedbackColour}>
              {feedback}
            </Text>
          </ModalBody>
          <ModalFooter>
            <Button
              colorScheme="yellow"
              isDisabled={validation}
              onClick={(event) => {
                SubscribeToAlertsEndpoint();
              }}
            >
              Subscribe
            </Button>
          </ModalFooter>
        </ModalContent>
      </Modal>
    </>
  );
}
