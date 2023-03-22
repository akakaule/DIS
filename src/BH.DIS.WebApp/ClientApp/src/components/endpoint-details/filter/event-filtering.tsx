import * as React from "react";
import * as api from "api-client";
import EventTypeFiltering from "./eventType-filtering";
import { Accordion, AccordionButton, AccordionIcon, AccordionItem, AccordionPanel, Box, Center, Flex, Grid, GridItem, HStack, Spacer } from "@chakra-ui/react";
import { Button, IconButton, Tooltip } from "@material-ui/core";
import SearchIcon from '@material-ui/icons/Search';
import FilterContext from "./filtering-context";
import SessionFiltering from "./session-filtering";
import EnqueuedFiltering from "./enqueued-filtering";
import UpdatedFiltering from "./updated-filtering";
import PayloadFiltering from "./payload-filtering";
import StatusFiltering from "./status-filtering";
import EventIdFiltering from "./eventId-filtering";

interface EventFilteringProps {
    handleFilterClicked: (eventFilter: api.EventFilter) => void;
}

const EventFiltering = (props: EventFilteringProps) => {
    const ctx = React.useContext(FilterContext);
    return (
        <>
            <Grid 
                templateColumns='repeat(5,1fr)'
                templateRows='repeat(2,fr)'
                gap='1'
            >
                <GridItem gridColumn={1} >
                    <EventTypeFiltering/>
                </GridItem>
                <GridItem gridColumn={2} >
                    <StatusFiltering/>
                </GridItem>
                <GridItem gridColumn={3} >
                    <EventIdFiltering/>
                </GridItem>
                <GridItem gridColumn={4} >
                    <SessionFiltering/>
                </GridItem>             
                <GridItem gridColumn={5} >
                    <Flex alignItems='center' direction='row' justify='end'>
                        <Button variant="outlined" endIcon={<SearchIcon fontSize="large" />} aria-label="search" size="medium" onClick={() => {
                            props.handleFilterClicked(ctx.filterContext);
                        }}>
                        Search
                        </Button>
                    </Flex>
                </GridItem>
                <GridItem paddingTop={3} gridRow={2} gridColumnStart={1} gridColumnEnd={6} >
                    <Accordion allowToggle>
                        <AccordionItem>
                            <h3>
                            <AccordionButton >
                                <Box flex={1} textAlign='center'>                                    
                                    Advanced Filters
                                    <AccordionIcon />  
                                </Box>       
                                                       
                            </AccordionButton>  
                           </h3>
                            <AccordionPanel>
                                <Grid 
                                    templateColumns='repeat(3,1fr)'                                    
                                    gap='2'
                                    >
                                    <GridItem gridColumn={1} >
                                        <EnqueuedFiltering/>
                                    </GridItem>
                                    <GridItem gridColumn={2}>
                                        <UpdatedFiltering/>
                                    </GridItem>
                                    <GridItem gridColumn={3}>
                                        <PayloadFiltering/>
                                    </GridItem>
                                </Grid>                                                      
                            </AccordionPanel>
                        </AccordionItem>                        
                    </Accordion>
                </GridItem>
            </Grid>
             <Spacer/>
            {/*<Grid
                templateRows='repeat(2, 1fr)'
                templateColumns='repeat(3, 1fr)'
                gap={2}
            >
                <GridItem gridRow={1} gridColumn={1} rowSpan={1} colSpan={1}>
                    <EventTypeFiltering/>
                    <div style={{ minHeight: 8 }}/>
                    <EventIdFiltering/>
                </GridItem>

                <GridItem gridRow={1} gridColumn={2} rowSpan={1} colSpan={1}>
                    <StatusFiltering/>
                    <div style={{ minHeight: 8 }}/>
                    <SessionFiltering/>
                </GridItem>

                <GridItem gridRow={1} gridColumn={3} rowSpan={1} colSpan={1}>
                    <Center>
                        <PayloadFiltering/>
                    </Center>
                </GridItem>
                <GridItem style={{ marginLeft: 12 }} gridRow={2} gridColumn={1} rowSpan={1} colSpan={1} >
                    <EnqueuedFiltering/>
                </GridItem>
                <GridItem style={{ marginLeft: 12 }} gridRow={2} gridColumn={2} rowSpan={1} colSpan={1} >
                    <UpdatedFiltering/>
                </GridItem>

                <GridItem gridRow={2} gridColumn={3} rowSpan={1} colSpan={1}>
                    <Center>
                        <Button variant="outlined" endIcon={<SearchIcon fontSize="large" />} aria-label="search" size="medium" onClick={() => {
                            props.handleFilterClicked(ctx.filterContext);
                        }}>
                            Search
                        </Button>
                    </Center>
                </GridItem>
            </Grid> */}
        </>)
}

export default EventFiltering;