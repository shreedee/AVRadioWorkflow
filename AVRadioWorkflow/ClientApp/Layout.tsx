import * as React from 'react';

import './siteWide.scss';
import { Navbar, Nav, Container } from 'react-bootstrap';

const MainNav: React.SFC<{}> = () => {
    return <Navbar bg="light" expand="lg">
        <Navbar.Brand href="/">AVRadio Workflow</Navbar.Brand>
        <Navbar.Toggle aria-controls="basic-navbar-nav" />
        <Navbar.Collapse id="basic-navbar-nav">
            <Nav className="mr-auto">
                <Nav.Link href="/foldercreator">Folder Creator</Nav.Link>
                <Nav.Link href="/publish">Publish</Nav.Link>
            </Nav>

        </Navbar.Collapse>
    </Navbar>;
}




class LayoutView extends React.Component<{}, {}> {

    public render() {

        return <Container>
            <MainNav />
            <div className="mainContent">
                {this.props.children}
            </div>
        </Container>;
    }
}

export default LayoutView;
