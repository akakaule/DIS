
import React from 'react';
import PropTypes from 'prop-types';
import TableHead from '@material-ui/core/TableHead';
import TableCell from '@material-ui/core/TableCell';
import { makeStyles, TableRow, TableSortLabel } from '@material-ui/core';
import { ITableHeadAction, ITableRow } from './data-table';
import { Flex, Text, Tooltip } from '@chakra-ui/react';
import { DataTableCheckbox } from './data-table-checkbox';
import Button from 'components/button';

export interface ITableHeadCell {
    id: string,
    label: string,
    numeric: boolean,
    width?: number,
}

interface IButtonState {
    isDisabled: boolean;
    text: string;
}
interface ITableHeaderProps {
    headCells: ITableHeadCell[];
    classes: Record<"table" | "root" | "paper" | "visuallyHidden", string>;
    onSelectAllClick: (event: any) => void;
    order: string;
    orderBy: string;
    numSelected: number;
    rowCount: number;
    onRequestSort: (...args: any[]) => any;
    selectedRows: ITableRow[];
    withCheckboxes: boolean;
    headerActions?: ITableHeadAction[];
    headerActionsWidth: number;
    setHeaderActionsWidth: (width: number) => void;
}

const useStyles = makeStyles((theme) => ({
    headCell: {
        backgroundColor: "#fff",
        position: "sticky",
        top: 0,
        boxShadow:
            "inset 0 0px 0 rgba(224, 224, 224, 1), inset 0 -.5px 0 rgba(224, 224, 224, 1)",
        zIndex: 10,
    },
}));

function DataTableHead(props: ITableHeaderProps) {
    const {
        onSelectAllClick,
        order,
        orderBy,
        numSelected,
        rowCount,
        onRequestSort,
    } = props;
    const classes = {
        ...props.classes,
        ...useStyles(),
    };
    const createSortHandler = (property: string) => (event: any) => {
        onRequestSort(event, property);
    };



    const checkboxColumn = (): JSX.Element | undefined => {
        if (props.withCheckboxes) {
            return (
                <TableCell
                    padding="checkbox"
                    variant="head"
                    style={{ width: "52px" }}
                    classes={{ root: classes.headCell }}
                >
                    <DataTableCheckbox
                        indeterminate={numSelected > 0 && numSelected < rowCount}
                        checked={rowCount > 0 && numSelected === rowCount}
                        onChange={onSelectAllClick}
                        inputProps={{ "aria-label": "select all desserts" }}
                    />
                </TableCell>
            );
        }
    };

    const dataColumns = (): JSX.Element[] | undefined => {
        return props.headCells.map((headCell, i) => {
            const first = i === 0;

            return (
                <TableCell
                    key={headCell.id}
                    padding={
                        first ? (props.withCheckboxes ? "none" : "normal") : undefined
                    }
                    sortDirection={
                        orderBy === headCell.id ? mapStringToAscOrDesc(order) : false
                    }
                    style={headCell.width ? { fontWeight: 600, width: headCell.width } : { fontWeight: 600 }}
                    variant="head"
                    classes={{ root: classes.headCell }}
                >
                    <TableSortLabel
                        active={orderBy === headCell.id}
                        direction={
                            orderBy === headCell.id ? mapStringToAscOrDesc(order) : "asc"
                        }
                        onClick={createSortHandler(headCell.id)}
                        style={{ minWidth: 0, display: "flex" }}
                    >
                        <Tooltip
                            label={headCell.label}
                            aria-label={headCell.label}
                            placement="bottom"
                        >
                            <Text noOfLines={1} >{headCell.label}</Text>
                        </Tooltip>
                        {orderBy === headCell.id ? (
                            <span className={classes.visuallyHidden}>
                                {order === "desc" ? "sorted descending" : "sorted ascending"}
                            </span>
                        ) : null}
                    </TableSortLabel>
                </TableCell>
            );
        });
    };

    const actionColumns = (): JSX.Element | undefined => {
        const cellPadding = 32;

        if (props.headerActions) {
            const buttonStatesInit: IButtonState[] = [];

            props.headerActions.forEach((action) => {
                buttonStatesInit.push({ text: action.name, isDisabled: false });
            });
            const [buttonStates, setButtonStates] = React.useState(buttonStatesInit);

            return (
                <TableCell
                    padding="normal"
                    variant="head"
                    width={`${props.headerActionsWidth + cellPadding}px`}
                    classes={{ root: classes.headCell }}
                >
                    <Flex justifyContent="flex-end" minWidth="fit-content">
                        {/* <ReactResizeDetector
                            handleWidth={true}
                            onResize={(width: number) => props.setHeaderActionsWidth(width)}
                        /> */}
                        {props.headerActions.map((action, i) => (
                            <Button
                                key={i}
                                isDisabled={
                                    props.selectedRows.length === 0 || buttonStates[i].isDisabled
                                }
                                marginRight={props.headerActions?.length === i + 1 ? 0 : 4}
                                onClick={(event: React.MouseEvent<any, MouseEvent>) => {
                                    buttonStates[i] = {
                                        text: `${buttonStates[i].text} OK`,
                                        isDisabled: true,
                                    };
                                    const clonedArray: IButtonState[] = [];
                                    buttonStates.forEach((val) =>
                                        clonedArray.push(val)
                                    );
                                    setButtonStates(clonedArray);
                                    event.preventDefault();
                                    event.stopPropagation();
                                    action.onClick(props.selectedRows);
                                }}
                            >
                                {buttonStates[i].text}
                            </Button>
                        ))}
                    </Flex>
                </TableCell>
            );
        }
    };

    return (
        <TableHead>
            <TableRow>
                {checkboxColumn()}
                {dataColumns()}
                {actionColumns()}
            </TableRow>
        </TableHead>
    );
}

function mapStringToAscOrDesc(order: string): "asc" | "desc" {
    return order === "asc" ? "asc" : "desc";
}

DataTableHead.propTypes = {
    classes: PropTypes.object.isRequired,
    numSelected: PropTypes.number.isRequired,
    onRequestSort: PropTypes.func.isRequired,
    onSelectAllClick: PropTypes.func.isRequired,
    order: PropTypes.oneOf(["asc", "desc"]).isRequired,
    orderBy: PropTypes.string.isRequired,
    rowCount: PropTypes.number.isRequired,
};

export default DataTableHead;