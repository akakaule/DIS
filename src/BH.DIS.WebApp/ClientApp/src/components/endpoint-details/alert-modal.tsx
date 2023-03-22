import React from "react";
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
} from "@chakra-ui/react";
import * as api from "api-client";
import { CUIAutoComplete } from "chakra-ui-autocomplete";
import { TextField } from "@material-ui/core";

interface IAlertModalProps {
  endpointId: string;
  isOpen: boolean;
  onClose: () => void;
  editable: boolean;
  subscription: api.EndpointSubscription | undefined;
}
export interface Item {
  label: string;
  value: string;
}

export default function AlertModal(props: IAlertModalProps) {
  const [eventTypes, setEventTypes] = React.useState<Item[]>([]);
  const [selectedEventTypes, setSelectedEventTypes] = React.useState<Item[]>([]);

  const client = new api.Client(api.CookieAuth());

  React.useEffect(() => {
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
    };
    fetchData();
  }, []);

  React.useEffect(() => {
    const tempSelectedEventTypes = props.subscription?.eventTypes?.map((x) => {
      return { label: x, value: x } as Item;
    });
    setSelectedEventTypes(tempSelectedEventTypes!);
  }, [props.subscription]);

  React.useEffect(() => {}, [props.isOpen, props.subscription]);
  return (
    <>
      {props.subscription && (
        <Modal isOpen={props.isOpen} onClose={props.onClose} isCentered={true}>
          <ModalOverlay />
          <ModalContent>
            <ModalHeader>
              Alert settings on {props.endpointId} for {props.subscription.authorId}
            </ModalHeader>
            <ModalCloseButton />
            <ModalBody>
              <Text fontWeight="bold" hidden={!(props.subscription.type === "teams")}>
                Url
              </Text>
              <Text fontWeight="bold" hidden={!(props.subscription.type === "mail")}>
                Email
              </Text>
              <Input isDisabled={!props.editable} hidden={!(props.subscription.type === "teams")} placeholder={props.subscription.url} />
              <Input isDisabled={!props.editable} hidden={!(props.subscription.type === "mail")} placeholder={props.subscription.mail} />
              <br />
              <br />
              <Text fontWeight="bold">Frequency</Text>
              <RadioGroup isDisabled={!props.editable} value={props.subscription.frequency?.toString()}>
                <Stack direction="row">
                  <Radio value="3600">Hourly</Radio>
                  <Radio value="86400">Daily</Radio>
                  <Radio value="604800">Weekly</Radio>
                </Stack>
              </RadioGroup>
              <br />
              <Text fontWeight="bold">Type</Text>
              <RadioGroup isDisabled={!props.editable} value={props.subscription.type}>
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
              <Text fontWeight="bold">Payload</Text>
              <TextField
                disabled={!props.editable}
                multiline={true}
                minRows={3}
                id="outlined-basic"
                variant="outlined"
                value={props.subscription.payload}
                margin="none"
                style={{ paddingLeft: "16", width: "-webkit-fill-available", height: "-webkit-fill-available" }}
              >
                {props.subscription.payload}
              </TextField>
              <br />
            </ModalBody>
            <ModalFooter></ModalFooter>
          </ModalContent>
        </Modal>
      )}
    </>
  );
}
