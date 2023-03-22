import { Tab, TabList, TabPanel, TabPanels, Tabs } from "@chakra-ui/react";
import { useLocation } from "react-router-dom";
import React from "react";
import { bunkerRed } from "shared-styles/colors";
import { number } from "prop-types";

export interface ITab {
  name: string;
  content: JSX.Element;
  isEnabled: boolean;
}

interface ITabSelectionsProps {
  tabs: ITab[];
}

const TabSelection: React.FunctionComponent<ITabSelectionsProps> = (props) => {
  const locationState = useLocation().state as { tabIndex: string };
  const index = locationState ? parseInt(locationState.tabIndex) : 0;

  return (
    <>
      {props?.tabs && (
        <Tabs isFitted={true} variant="enclosed" width="100%" display="flex" flexDirection="column" padding="1rem" border="1px solid rgba(224, 224, 224, 1)" borderRadius="4px" defaultIndex={isNaN(index) ? 0 : index}>
          <TabList mb="1em">{props.tabs?.map((tab) => createTab(tab.name, tab.isEnabled))}</TabList>
          <TabPanels display="flex" minHeight="0">
            {props.tabs?.map((tab, idx) => createTabPanel(tab.content, idx))}
          </TabPanels>
        </Tabs>
      )}
    </>
  );
};

const createTab = (name: string, isEnabled: boolean): JSX.Element => {
  return (
    <Tab key={"tab-" + name} isDisabled={!isEnabled} fontWeight="600" _selected={{ color: "white", bg: bunkerRed }} _hover={{ bg: "gray.100" }}>
      {name}
    </Tab>
  );
};

const createTabPanel = (content: JSX.Element, idx: number): JSX.Element => {
  return (
    <TabPanel key={"tabpanel-" + content.key} display="flex" overflow="auto" width="100%">
      {content}
    </TabPanel>
  );
};

export default TabSelection;
