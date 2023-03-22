import * as React from "react";
import * as api from "api-client";
import { makeStyles } from "@material-ui/core/styles";
import {
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
  ButtonGroup,
  useDisclosure
} from "@chakra-ui/react";
import Table from "@material-ui/core/Table";
import TableBody from "@material-ui/core/TableBody";
import TableCell from "@material-ui/core/TableCell";
import TableRow from "@material-ui/core/TableRow";
import Button from "components/button";
import { formatMoment } from "functions/endpoint.functions";
import { Link } from "@material-ui/core";
import { useEffect } from "react";

interface IMessageListingProps {
  eventDetails: api.Event | undefined;
  eventTypes: api.EventType[];
  skipEvent: (eventId: string, messageId: string) => void;
  resubmitEvent: (eventId: string, messageId: string) => void;
  resubmitEventWithChanges: (
    eventId: string,
    messageId: string,
    body: api.ResubmitWithChanges
  ) => void;
}

interface IButtonState {
  isDisabled: boolean;
  text: string;
}

export default function MessageListing(props: IMessageListingProps) {
  const useStyles = makeStyles({
    table: {
      width: "auto",
      flex: "1"
    },
  });

  const [showDetails, setShowDetails] = React.useState(false);
  const handleDetailsToggle = () => setShowDetails(!showDetails);
  const { isOpen, onOpen, onClose } = useDisclosure();
  const classes = useStyles();
  const [textAreaValue, setTextAreaValue] = React.useState(
    props.eventDetails?.messageContent?.eventContent?.eventJson
  );
  const [eventTypeIdValue, setEventTypeIdValue] = React.useState(
    props.eventDetails?.eventTypeId
  );

  useEffect(() => {
    setTextAreaValue(props.eventDetails?.messageContent?.eventContent?.eventJson);
  }, [props.eventDetails]);

  const resubmitWithChanges: IButtonState = {
    isDisabled: false,
    text: "Resubmit with changes",
  };
  const [
    resubmitWithChangesButton,
    setResubmitWithChangesButton,
  ] = React.useState(resubmitWithChanges);

  const resubmit: IButtonState = { isDisabled: false, text: "Resubmit" };
  const [resubmitButton, setResubmitButton] = React.useState(resubmit);

  const skip: IButtonState = { isDisabled: false, text: "Skip" };
  const [skipButton, setSkipButton] = React.useState(skip);

  const handleInputChange = (e: any) => {
    const inputValue = e.target.value;
    setTextAreaValue(inputValue);
  };

  const handleEventTypeIdChange = (e: any) => {
    setEventTypeIdValue(e.target.value);
  };

  const isFailedMessage = (status: string | undefined): boolean => {
    if (!status) return false;
    const lowerStatus = status.toLowerCase();
    return lowerStatus === "failed" || lowerStatus === "unsupported" || lowerStatus === "deadlettered";
  }

  const isDeadletteredMessage = (status: string | undefined): boolean => {
    if (!status) return false;
    const lowerStatus = status.toLowerCase();
    return lowerStatus === "deadlettered";
  }

  const skipEventClick = () => {
    setSkipButton({ text: "Skipped", isDisabled: true });
    props.skipEvent(
      props.eventDetails?.eventId!,
      props.eventDetails?.lastMessageId!
    );
  };

  const resubmitEventClick = () => {
    setResubmitButton({ text: "Resubmitted", isDisabled: true });
    props.resubmitEvent(
      props.eventDetails?.eventId!,
      props.eventDetails?.lastMessageId!
    );
  };

  const resubmitEventWithChangesClick = () => {
    onClose();
    setResubmitWithChangesButton({ text: "Resubmitted", isDisabled: true });
    const body: api.ResubmitWithChanges = api.ResubmitWithChanges.fromJS({
      eventTypeId: eventTypeIdValue,
      eventContent: textAreaValue,
    });
    props.resubmitEventWithChanges(
      props.eventDetails?.eventId!,
      props.eventDetails?.lastMessageId!,
      body
    );
  };

  return (
    <div>
      <Heading as="h4" size="md">
        Details
        {isFailedMessage(props.eventDetails?.resolutionStatus) && <ButtonGroup size="xs" spacing={4} margin="0 1rem">
          <Button
            isDisabled={resubmitButton.isDisabled}
            onClick={resubmitEventClick}
          >
            {resubmitButton.text}
          </Button>
          <Button
            isDisabled={resubmitWithChangesButton.isDisabled}
            onClick={onOpen}
          >
            {resubmitWithChangesButton.text}
          </Button>{" "}
          <Button
            isDisabled={skipButton.isDisabled}
            onClick={skipEventClick}
          >
            {skipButton.text}
          </Button>{" "}
        </ButtonGroup>}
      </Heading>
      <br />
      <Flex flexDirection={"column"}>
        <Table className={classes.table} size="small" aria-label="a dense table" style={{ marginRight: '1rem' }}>
          <TableBody>
            <TableRow hover={true}>
              <TableCell>
                <b>EventId</b>
              </TableCell>
              <TableCell>{props.eventDetails?.eventId}</TableCell>
            </TableRow>
            <TableRow hover={true}>
              <TableCell>
                <b>EventTypeId</b>
              </TableCell>
              <TableCell>
                {props.eventDetails?.eventTypeId}
              </TableCell>
            </TableRow>
            <TableRow hover={true}>
              <TableCell>
                <b>SessionId</b>
              </TableCell>
              <TableCell>{props.eventDetails?.sessionId}</TableCell>
            </TableRow>
            <TableRow hover={true}>
              <TableCell>
                <b>EndpointId</b>
              </TableCell>
              <TableCell>
                {props.eventDetails?.endpointId && <Link href={`/Endpoints/Details/${props.eventDetails?.endpointId}`}>{props.eventDetails?.endpointId}</Link>}
              </TableCell>
            </TableRow>
            <TableRow hover={true}>
              <TableCell>
                <b>Status</b>
              </TableCell>
              <TableCell>{props.eventDetails?.resolutionStatus}</TableCell>
            </TableRow>
            <TableRow hover={true}>
              <TableCell>
                <b>MessageType</b>
              </TableCell>
              <TableCell>
                {props.eventDetails?.messageType}
              </TableCell>
            </TableRow>
            <TableRow hover={true}>
              <TableCell>
                <b>MessageId</b>
              </TableCell>
              <TableCell>
                {props.eventDetails?.lastMessageId}
              </TableCell>
            </TableRow>
            <TableRow hover={true} hidden={!showDetails}>
              <TableCell>
                <b>Enqueued Time (UTC)</b>
              </TableCell>
              <TableCell>
                {formatMoment(
                  props.eventDetails?.enqueuedTimeUtc
                )}
              </TableCell>
            </TableRow>
            <TableRow hover={true} hidden={!showDetails}>
              <TableCell>
                <b>From</b>
              </TableCell>
              <TableCell>{props.eventDetails?.from}</TableCell>
            </TableRow>
            <TableRow hover={true} hidden={!showDetails}>
              <TableCell>
                <b>To</b>
              </TableCell>
              <TableCell>{props.eventDetails?.to}</TableCell>
            </TableRow>
            <TableRow hover={true} hidden={!showDetails}>
              <TableCell>
                <b>EndpointRole</b>
              </TableCell>
              <TableCell>
                {props.eventDetails?.endpointRole}
              </TableCell>
            </TableRow>
            <TableRow hover={true} hidden={!showDetails}>
              <TableCell>
                <b>OriginatingMessageId</b>
              </TableCell>
              <TableCell>{props.eventDetails?.originatingMessageId}</TableCell>
            </TableRow>
          </TableBody>
        </Table>
      </Flex>
      <br />
      <Button
        onClick={handleDetailsToggle}
      >
        {!showDetails ? "Show details" : "Hide details"}
      </Button>
      <br />
      <Heading as="h4" size="md">
        Payload
      </Heading>
      <br />
      <pre>{props.eventDetails?.messageContent ? JSON.stringify(JSON.parse(props.eventDetails?.messageContent?.eventContent?.eventJson!), null, 2) : ""} </pre>
      {isFailedMessage(props.eventDetails?.resolutionStatus) && <>
        <Divider />
        <br />
        <Heading as="h4" size="md" hidden={props.eventDetails?.messageContent?.eventContent?.eventJson! === undefined}>
          Error
        </Heading>
        <br />
        <Table size="small" aria-label="a dense table" hidden={props.eventDetails?.messageContent?.errorContent === undefined}>
          {!isDeadletteredMessage(props.eventDetails?.resolutionStatus) ?
            <TableBody>
              <TableRow hover={true}>
                <TableCell>
                  <b>Error Text</b>
                </TableCell>
                <TableCell>
                  {props.eventDetails?.messageContent?.errorContent?.errorText}
                </TableCell>
              </TableRow>
              <TableRow hover={true}>
                <TableCell>
                  <b>Error Type</b>
                </TableCell>
                <TableCell>
                  {props.eventDetails?.messageContent?.errorContent?.errorType}
                </TableCell>
              </TableRow>
              <TableRow hover={true}>
                <TableCell>
                  <b>Exception Source</b>
                </TableCell>
                <TableCell>
                  {props.eventDetails?.messageContent?.errorContent?.exceptionSource}
                </TableCell>
              </TableRow>
              <TableRow hover={true}>
                <TableCell variant={"head"}>
                  <b>Exception</b>
                </TableCell>
                <TableCell >
                  {
                    props.eventDetails?.messageContent?.errorContent
                      ?.exceptionStackTrace
                  }
                </TableCell>
              </TableRow>
            </TableBody> :
            <TableBody>
              <TableRow hover={true}>
                <TableCell>
                  <b>DeadLetter Reason</b>
                </TableCell>
                <TableCell>
                  {props.eventDetails?.deadLetterReason}
                </TableCell>
              </TableRow>
              <TableRow hover={true}>
                <TableCell>
                  <b>DeadLetter Error Description</b>
                </TableCell>
                <TableCell>
                  {props.eventDetails?.deadLetterErrorDescription}
                </TableCell>
              </TableRow>
            </TableBody>}
        </Table>
        <br />
      </>}

      <Heading as="h4" size="md" hidden={props.eventDetails?.messageContent?.errorContent !== undefined}>
        Event sucessfully completed!
      </Heading>
      <br />
      <Table size="small" aria-label="a dense table" hidden={props.eventDetails?.messageContent?.errorContent !== undefined}>
        <TableBody>
          <TableRow hover={true}>
            <TableCell>
              <b>Receiving endpoint</b>
            </TableCell>
            <TableCell>
              {props.eventDetails?.to}
            </TableCell>
          </TableRow>
          <TableRow hover={true}>
            <TableCell>
              <b>Publishing endpoint</b>
            </TableCell>
            <TableCell>
              {props.eventDetails?.from}
            </TableCell>
          </TableRow>
          <TableRow hover={true}>
            <TableCell>
              <b>Completed Time (UTC)</b>
            </TableCell>
            <TableCell>
              {formatMoment(
                props.eventDetails?.enqueuedTimeUtc
              )}
            </TableCell>
          </TableRow>
        </TableBody>
      </Table>

      <Modal isOpen={isOpen} onClose={onClose} size="xl">
        <ModalOverlay />
        <ModalContent>
          <ModalHeader>Resubmit with changes</ModalHeader>
          <ModalCloseButton />
          <ModalBody>
            Original event:
            <pre>{props.eventDetails?.messageContent?.eventContent?.eventJson ? JSON.stringify(JSON.parse(props.eventDetails?.messageContent?.eventContent?.eventJson), null, 2) : ""}</pre>
            <br />
            Event type:
            <Select onChange={handleEventTypeIdChange}>
              {props.eventTypes.map((et) => (
                <option key={et.id}
                  selected={
                    props.eventDetails?.eventTypeId === et.id
                  }
                  value={et.id}
                >
                  {et.id}
                </option>
              ))}
            </Select>
            Modified event:
            <Textarea
              value={JSON.stringify(JSON.parse(textAreaValue ? textAreaValue : ""),null,2)}
              onChange={handleInputChange}
              size="sm"
            />
          </ModalBody>

          <ModalFooter>
            <Button colorScheme="blue" mr={4} variant="outline" onClick={onClose}>
              Close
            </Button>
            <Button onClick={resubmitEventWithChangesClick}>Resubmit</Button>
          </ModalFooter>
        </ModalContent>
      </Modal>
    </div>
  );
}
