import React from "react";
import PropTypes from "prop-types";
import { withStyles } from "@material-ui/core/styles";
import Toolbar from "@material-ui/core/Toolbar";
import { Button, Flex, FormLabel, Icon, Input, InputGroup, InputLeftElement, Switch } from "@chakra-ui/react";
import EndpointAlertsButton from "components/endpoint-list/endpoint-alerts-button";
import EndpointRoleAssignmentsButton from "components/endpoint-list/endpoint-roleassignments-button";
import FilterListIcon from "@material-ui/icons/FilterList";
import DataTableEndpointFilter from "./data-table-endpoint-filter";

const StyledToolBar = withStyles((theme) => ({
  root: {
    padding: "0 16px",
  },
}))(Toolbar);

interface IDataTableToolbar {
  filterFunction: (filterString: string) => void;
  subscribe?: string;
  endpointIds?: Array<string>;
  checkedEndpointIds?: Array<string>;
  roleAssignmentscript: string;
  checked?: (name: string, state: boolean) => void;
}

const DataTableToolbar = (props: IDataTableToolbar) => {
  return (
    <StyledToolBar>
      <Flex justifyContent="space-between" width="100%">
        <InputGroup width="50%">
          <InputLeftElement children={<Icon name="search" color="gray.300" />} />
          <Input variant="outline" type="text" placeholder="Search" onChange={(event: any) => props.filterFunction(event.target.value)} />
        </InputGroup>
        <Flex justify="center" align="center">
          {props.endpointIds ? <DataTableEndpointFilter endpointIds={props.endpointIds} checkedEndpointIds={props.checkedEndpointIds} checked={props.checked} /> : null}
          {props.subscribe ? (
            <React.Fragment>
              <EndpointAlertsButton endpointId={props.subscribe} />
              &nbsp;
              <EndpointRoleAssignmentsButton endpointId={props.subscribe} script={props.roleAssignmentscript} />
            </React.Fragment>
          ) : null}
        </Flex>
      </Flex>
    </StyledToolBar>
  );
};

DataTableToolbar.propTypes = {
  numSelected: PropTypes.number.isRequired,
};

export default DataTableToolbar;
